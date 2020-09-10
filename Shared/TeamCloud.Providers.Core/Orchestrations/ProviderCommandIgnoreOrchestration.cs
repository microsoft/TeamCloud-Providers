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
    public static class ProviderCommandIgnoreOrchestration
    {
        [FunctionName(nameof(ProviderCommandIgnoreOrchestration))]
        public static ICommandResult RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new System.ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<IProviderCommand>();
            var commandResult = command.CreateResult();

            commandResult.Errors.Add(new CommandError()
            {
                Message = $"Command '{command.GetType().Name}' is ignored.",
                Severity = CommandErrorSeverity.Warning
            });

            return commandResult;
        }
    }
}
