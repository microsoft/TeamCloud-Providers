/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.DevOps.Orchestrations;
namespace TeamCloud.Providers.Azure.DevOps
{
    public class CommandTrigger
    {
        [FunctionName(nameof(CommandTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "command")] HttpRequest httpRequest,
            [DurableClient] IDurableClient durableClient,
            ILogger logger)
        {
            if (httpRequest is null) throw new ArgumentNullException(nameof(httpRequest));
            if (durableClient is null) throw new ArgumentNullException(nameof(durableClient));

            var commandJson = await httpRequest.ReadAsStringAsync().ConfigureAwait(false);
            logger.LogInformation($"Received: {commandJson}");
            var callbackUrl = httpRequest.Headers.GetCallbackUrl();

            var command = JsonConvert.DeserializeObject<ICommand>(commandJson);
            string orchestrationName = null;
            switch (command)
            {
                case ProjectCreateCommand projectCreateCommand:
                    orchestrationName = nameof(ProjectCreateOrchestration);
                    break;
                case ProjectUpdateCommand projectCreateCommand:
                    orchestrationName = nameof(ProjectUpdateOrchestration);
                    break;
                case ProjectDeleteCommand projectCreateCommand:
                    orchestrationName = nameof(ProjectDeleteOrchestration);
                    break;
                default:
                    throw new ArgumentException("Unknown command was provided as input.");
            }

            var instanceId = await durableClient
                .StartNewAsync<OrchestrationRequest>(orchestrationName, command.CommandId.ToString(), new OrchestrationRequest { Command = command, CallbackUrl = callbackUrl })
                .ConfigureAwait(false);

            var status = await durableClient
                .GetStatusAsync(command.CommandId.ToString())
                .ConfigureAwait(false);

            var result = status.GetResult();

            return new AcceptedResult(string.Empty, result);
        }
    }
}