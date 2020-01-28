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
using TeamCloud.Providers.Azure.DevTestLabs.Orchestrations;

namespace TeamCloud.Providers.Azure.DevTestLabs
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

            var instanceId = await durableClient
                .StartNewAsync<OrchestrationRequest>(OrchestrationName(command), command.CommandId.ToString(), new OrchestrationRequest { Command = command, CallbackUrl = callbackUrl })
                .ConfigureAwait(false);

            var status = await durableClient
                .GetStatusAsync(command.CommandId.ToString())
                .ConfigureAwait(false);

            var result = status.GetResult();

            return new AcceptedResult(string.Empty, result);
        }

        private static string OrchestrationName(ICommand command) => (command) switch
        {
            ProjectCreateCommand projectCreateCommand => nameof(ProjectCreateOrchestration),
            ProjectUpdateCommand projectCreateCommand => nameof(ProjectUpdateOrchestration),
            ProjectDeleteCommand projectCreateCommand => nameof(ProjectDeleteOrchestration),
            _ => throw new NotSupportedException()
        };
    }
}
