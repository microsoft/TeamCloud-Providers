/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Results;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Orchestrations
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

            var commandOrchestration = await functionContext
                .CallActivityAsync<string>(nameof(ProviderCommandMessageDispatcher), commandMessage)
                .ConfigureAwait(true);

            var commandResult = await functionContext
                .CallSubOrchestratorAsync<ICommandResult>(commandOrchestration, commandMessage.CommandId.ToString(), commandMessage.Command)
                .ConfigureAwait(true);

            await functionContext
                .CallActivityAsync(nameof(ProviderCommandResultMessageQueueWriter), (commandMessage, commandMessage.CreateResultMessage(commandResult)))
                .ConfigureAwait(true);
        }
    }
}
