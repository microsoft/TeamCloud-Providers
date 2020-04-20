/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core.Orchestrations
{
    public static class ProviderCommandFallbackOrchestration
    {
        [FunctionName(nameof(ProviderCommandFallbackOrchestration))]
        public static ICommandResult RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new System.ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<IProviderCommand>();
            var commandResult = command.CreateResult();

            return commandResult;
        }
    }
}
