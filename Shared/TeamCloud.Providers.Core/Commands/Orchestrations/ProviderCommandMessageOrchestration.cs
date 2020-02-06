/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Core.Commands.Results;

namespace TeamCloud.Providers.Core.Commands.Orchestrations
{
    public class ProviderCommandMessageOrchestration
    {
        [FunctionName(nameof(ProviderCommandMessageOrchestration))]
        public async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            var commandMessage = functionContext
                .GetInput<ProviderCommandMessage>();

            var commandResult = commandMessage
                .Command
                .CreateResult();

            var commandOrchestration = await functionContext
                .CallActivityAsync<string>(nameof(ProviderCommandMessageDispatcher), commandMessage)
                .ConfigureAwait(true);

            if (!string.IsNullOrEmpty(commandOrchestration))
            {
                commandResult = await functionContext
                    .CallSubOrchestratorAsync<ICommandResult>(commandOrchestration, commandMessage.CommandId.ToString(), commandMessage.Command)
                    .ConfigureAwait(true);
            }

            await functionContext
                .CallActivityAsync(nameof(ProviderCommandResultQueueWriter), (commandMessage, commandResult))
                .ConfigureAwait(true);
        }
    }
}
