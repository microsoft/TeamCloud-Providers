/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.GitHub.Actions.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.GitHub.Actions.Orchestrations
{
    public static class CommandDispatchOrchestration
    {
        [FunctionName(nameof(CommandDispatchOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<IProviderCommand>();
            var commandResult = command.CreateResult();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            using (log.BeginCommandScope(command))
            {
                try
                {
                    functionContext.SetCustomStatus("Dispatching workflow", commandLog);

                    await functionContext
                        .CallActivityWithRetryAsync(nameof(CommandDispatchActivity), command)
                        .ConfigureAwait(true);

                    functionContext.SetCustomStatus("Waiting for workflow to complete", commandLog);

                    commandResult = await functionContext
                        .WaitForExternalEvent<ICommandResult>(command.CommandId.ToString(), CommandResult.MaximumTimeout)
                        .ConfigureAwait(true);

                    functionContext.SetCustomStatus("Workflow completed", commandLog);

                    commandResult.Result = new ProviderOutput();
                }
                catch (Exception exc)
                {
                    commandResult ??= command.CreateResult();
                    commandResult.Errors.Add(exc);

                    throw exc.AsSerializable();
                }
                finally
                {
                    var commandException = commandResult.Errors?.ToException();

                    if (commandException is null)
                        functionContext.SetCustomStatus($"Command succeeded", commandLog);
                    else
                        functionContext.SetCustomStatus($"Command failed: {commandException.Message}", commandLog, commandException);

                    functionContext.SetOutput(commandResult);
                }
            }
        }
    }
}
