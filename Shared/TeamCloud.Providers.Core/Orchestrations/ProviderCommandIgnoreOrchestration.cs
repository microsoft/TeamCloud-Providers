/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Providers.Core.Model;

namespace TeamCloud.Providers.Core.Orchestrations
{
    public static class ProviderCommandIgnoreOrchestration
    {
        [FunctionName(nameof(ProviderCommandIgnoreOrchestration))]
        public static ICommandResult RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new System.ArgumentNullException(nameof(functionContext));

            var commandContext = functionContext.GetInput<ProviderCommandContext>();
            var commandResult = commandContext.Command.CreateResult();

            commandResult.Errors.Add(new CommandError()
            {
                Message = $"Command '{commandContext.Command.GetType().Name}' is ignored.",
                Severity = CommandErrorSeverity.Warning
            });

            return commandResult;
        }
    }
}
