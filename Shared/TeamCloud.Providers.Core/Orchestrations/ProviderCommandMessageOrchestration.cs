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
using TeamCloud.Providers.Azure.AppInsights;
using TeamCloud.Providers.Core.Activities;

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

                var commandOrchestrationInstanceId = CommandHandler.GetCommandOrchestrationInstanceId(command);

                if (string.IsNullOrEmpty(commandOrchestrationName))
                {
                    commandLog
                        .LogInformation($"Dispatching command '{command.GetType().FullName}' ({command.CommandId}) >>> FALLBACK ({commandOrchestrationInstanceId})");

                    commandResult = await functionContext
                        .CallSubOrchestratorWithRetryAsync<ICommandResult>(nameof(ProviderCommandFallbackOrchestration), commandOrchestrationInstanceId, command)
                        .ConfigureAwait(true);
                }
                else
                {
                    commandLog
                        .LogInformation($"Dispatching command '{command.GetType().FullName}' ({commandMessage.CommandId}) >>> {commandOrchestrationName} ({commandOrchestrationInstanceId})");

                    commandResult = await functionContext
                        .CallSubOrchestratorWithRetryAsync<ICommandResult>(commandOrchestrationName, commandOrchestrationInstanceId, command)
                        .ConfigureAwait(true);
                }

                commandResult = await functionContext
                    .CallActivityWithRetryAsync<ICommandResult>(nameof(ProviderCommandResultAugmentActivity), (commandResult, commandOrchestrationInstanceId))
                    .ConfigureAwait(true);
            }
            catch (Exception exc)
            {
                commandLog.LogError(exc, $"Processing command '{command.GetType().FullName}' ({command.CommandId}) Failed >>> {exc.Message}");

                commandResult ??= command.CreateResult();
                commandResult.Errors.Add(exc);
            }
            finally
            {
                try
                {
                    await functionContext
                        .CallActivityWithRetryAsync(nameof(ProviderCommandResultSendActivity), (commandResult, commandMessage.CallbackUrl))
                        .ConfigureAwait(true);
                }
                catch (Exception exc)
                {
                    commandLog.LogError(exc, $"Sending result for command '{command.GetType().FullName}' ({command.CommandId}) Failed >>> {exc.Message}");

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
