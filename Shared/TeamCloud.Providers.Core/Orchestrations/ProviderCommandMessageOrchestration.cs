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
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core.Activities;
using TeamCloud.Providers.Core.API;

namespace TeamCloud.Providers.Core.Orchestrations
{
    public static class ProviderCommandMessageOrchestration
    {
        [FunctionName(nameof(ProviderCommandMessageOrchestration))]
        public static async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var commandMessage = functionContext.GetInput<ProviderCommandMessage>()
                ?? throw new ArgumentException("Command message is null", nameof(functionContext));

            var command = commandMessage.Command
                ?? throw new ArgumentException("Command message does not contain a command", nameof(functionContext));

            var commandResult = command.CreateResult();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            try
            {
                await functionContext
                    .AuditAsync(command, commandResult)
                    .ConfigureAwait(true);

                var commandOrchestrationName = await functionContext
                    .CallActivityWithRetryAsync<string>(nameof(ProviderCommandDispatchActivity), command)
                    .ConfigureAwait(true);

                commandOrchestrationName ??= nameof(ProviderCommandIgnoreOrchestration);

                var commandOrchestrationInstanceId = CommandTrigger.GetCommandOrchestrationInstanceId(command);

                commandLog
                    .LogInformation($"Dispatching command '{command.GetType()}' ({commandMessage.CommandId}) >>> {commandOrchestrationName} ({commandOrchestrationInstanceId})");

                commandResult = await functionContext
                    .CallSubOrchestratorWithRetryAsync<ICommandResult>(commandOrchestrationName, commandOrchestrationInstanceId, command)
                    .ConfigureAwait(true);

                var timeoutDuration = TimeSpan.FromMinutes(5);
                var timeout = functionContext.CurrentUtcDateTime.Add(timeoutDuration);

                while (true)
                {
                    // there is a chance that the suborchestration used to agument the command result
                    // doesn't reflect the final runtime status (completed / failed / canceled) because
                    // of timing issues in the durable functions runtime. to void a none final runtime 
                    // status reported back to the orchestrator we loop / wait for this runtime status.

                    commandResult = await functionContext
                        .CallActivityWithRetryAsync<ICommandResult>(nameof(ProviderCommandResultAugmentActivity), (commandResult ?? command.CreateResult(), commandOrchestrationInstanceId))
                        .ConfigureAwait(true);

                    if (commandResult.RuntimeStatus.IsFinal())
                    {
                        // the command orchestration finally reached a final state, so we can stop
                        // polling the status of the command orchestration to augment the command result

                        break;
                    }
                    else if (functionContext.CurrentUtcDateTime > timeout)
                    {
                        // this should never happen, but we need to be bulletproof here.
                        // the command orchestration finished, but we weren't able to resolve
                        // an orchstration status with a final runtime status to finish
                        // the overall command procession

                        throw new OperationCanceledException($"Command orchestration '{commandOrchestrationInstanceId}' failed to deliver a final runtime status.");
                    }
                }
            }
            catch (Exception exc)
            {
                commandLog.LogError(exc, $"Processing command '{command.GetType()}' ({command.CommandId}) Failed >>> {exc.Message}");

                commandResult ??= command.CreateResult();
                commandResult.Errors.Add(exc);
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(commandMessage.CallbackUrl))
                    {
                        // try to send back the command result back to the callback url
                        // this operation is part of the overall command processing and 
                        // exceptions caused by this operation will be part of the 
                        // command result (and will be returned if the orchestrator 
                        // fetches the command result via a GET request)

                        await functionContext
                            .CallActivityWithRetryAsync(nameof(ProviderCommandResultSendActivity), (commandResult, commandMessage.CallbackUrl))
                            .ConfigureAwait(true);
                    }
                }
                catch (Exception exc)
                {
                    commandLog.LogError(exc, $"Sending result for command '{command.GetType()}' ({command.CommandId}) Failed >>> {exc.Message}");

                    commandResult ??= command.CreateResult();
                    commandResult.Errors.Add(exc);
                }
                finally
                {
                    await functionContext
                        .AuditAsync(command, commandResult)
                        .ConfigureAwait(true);

                    functionContext.SetOutput(commandResult);
                }
            }
        }
    }
}
