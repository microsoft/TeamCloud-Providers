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
             [HttpTrigger(AuthorizationLevel.Function, "post", Route = "command")] ProviderCommand providerCommand,
            [DurableClient] IDurableClient durableClient,
            ILogger logger)
        {
            if (providerCommand is null)
                throw new ArgumentNullException(nameof(providerCommand));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var orchestrationName = OrchestrationName(providerCommand.Command);

            _ = await durableClient
                .StartNewAsync<object>(orchestrationName, providerCommand.CommandId.ToString(), providerCommand)
                .ConfigureAwait(false);

            var status = await durableClient
                .GetStatusAsync(providerCommand.CommandId.ToString())
                .ConfigureAwait(false);

            var providerCommandResult = providerCommand.CreateResult(status);

            if (providerCommandResult.RuntimeStatus.IsFinal())
            {
                return new OkObjectResult(providerCommandResult);
            }

            return new AcceptedResult(string.Empty, providerCommandResult);
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
