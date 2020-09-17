/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamCloud.Audit;
using TeamCloud.Configuration;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core.Activities;
using TeamCloud.Providers.Core.API;
using TeamCloud.Providers.Core.Configuration;
using TeamCloud.Providers.Core.Model;

namespace TeamCloud.Providers.Core
{
    public static class GlobalExtensions
    {
        public static ICommandResult ApplyStatus(this ICommandResult commandResult, DurableOrchestrationStatus orchestrationStatus)
        {
            if (commandResult is null)
                throw new ArgumentNullException(nameof(commandResult));

            if (orchestrationStatus is null)
                throw new ArgumentNullException(nameof(orchestrationStatus));

            commandResult.CreatedTime = GetNullWhenMinValue(orchestrationStatus.CreatedTime);
            commandResult.LastUpdatedTime = GetNullWhenMinValue(orchestrationStatus.LastUpdatedTime);
            commandResult.RuntimeStatus = (CommandRuntimeStatus)orchestrationStatus.RuntimeStatus;
            commandResult.CustomStatus = orchestrationStatus.CustomStatus?.ToString();

            return commandResult;

            static DateTime? GetNullWhenMinValue(DateTime dateTime)
                => (dateTime == DateTime.MinValue ? default(DateTime?) : dateTime);
        }

        public static Task AuditAsync(this IDurableOrchestrationContext functionContext, ICommand command, ICommandResult commandResult = default)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            return functionContext.CallActivityWithRetryAsync(nameof(ProviderCommandAuditActivity), (command, commandResult));
        }

        public static IServiceCollection AddTeamCloudCommandOrchestration(this IServiceCollection services, Action<IOrchestrationConfiguration> config)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            if (config is null)
                throw new ArgumentNullException(nameof(config));

            services
                .AddTeamCloudOptions(Assembly.GetExecutingAssembly())
                .AddTeamCloudAudit();

            services
                .TryAddSingleton<IOrchestrationConfiguration>(new OrchestrationConfiguration());

            var configuration = services
                .BuildServiceProvider()
                .GetRequiredService<IOrchestrationConfiguration>();

            if (configuration is OrchestrationConfiguration configurationTyped)
                config(configurationTyped);

            return services;
        }

        public static async Task<ICommand> GetCommandAsync(this IDurableClient durableClient, Guid commandId)
        {
            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var commandStatus = await durableClient
                .GetStatusAsync(CommandTrigger.GetCommandOrchestrationInstanceId(commandId))
                .ConfigureAwait(false);

            if (commandStatus?.Input?.HasValues ?? false)
            {
                var command = commandStatus.Input
                    .ToObject<ICommand>();

                if (command != null)
                    return command;
            }

            commandStatus = await durableClient
                .GetStatusAsync(CommandTrigger.GetCommandMessageOrchestrationInstanceId(commandId))
                .ConfigureAwait(false);

            if (commandStatus?.Input?.HasValues ?? false)
            {
                var (commandMessage, commandContext) = commandStatus.Input
                    .ToObject<(ProviderCommandMessage, ProviderCommandContext)>();

                return commandMessage.Command ?? commandContext.Command;
            }

            return null;
        }

        public static async Task<ICommandResult> GetCommandResultAsync(this IDurableClient durableClient, Guid commandId)
        {
            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var command = await durableClient
                .GetCommandAsync(commandId)
                .ConfigureAwait(false);

            if (command is null)
                return null;

            return await durableClient
                .GetCommandResultAsync(command)
                .ConfigureAwait(false);
        }

        public static async Task<ICommandResult> GetCommandResultAsync(this IDurableClient durableClient, ICommand command)
        {
            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var commandStatus = await durableClient
                .GetStatusAsync(CommandTrigger.GetCommandOrchestrationInstanceId(command))
                .ConfigureAwait(false);

            if (commandStatus is null)
            {
                // the command orchestration wasn't created yet
                // there is no way to return a command result

                return null;
            }
            else if (commandStatus.RuntimeStatus.IsFinal())
            {
                // the command orchestration reached a final state
                // but the message orchestration is responsible to
                // send the result and there could modify the overall
                // command result (e.g. if a send operation fails).

                var commandMessageStatus = await durableClient
                    .GetStatusAsync(CommandTrigger.GetCommandMessageOrchestrationInstanceId(command))
                    .ConfigureAwait(false);

                if (commandMessageStatus?.Output.HasValues ?? false)
                {
                    return commandMessageStatus.Output
                        .ToObject<ICommandResult>()
                        .ApplyStatus(commandMessageStatus);
                }
            }

            // the command orchestration is in-flight
            // get the current command result from its
            // output or fallback to the default result

            var commandResult = commandStatus.Output.HasValues
                ? commandStatus.Output.ToObject<ICommandResult>()
                : command.CreateResult(); // fallback to the default command result

            return commandResult.ApplyStatus(commandStatus);
        }

        public static Task SetInstrumentationKeyAsync(this IDurableOrchestrationContext functionContext, Guid instrumentationKey)
            => instrumentationKey.ToString().Equals(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"), StringComparison.OrdinalIgnoreCase)
            ? Task.CompletedTask : functionContext.SetAppSettingAsync("APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey.ToString());

        public static Task SetAppSettingAsync(this IDurableOrchestrationContext functionContext, string key, string value = default)
            => functionContext.CallActivityWithRetryAsync(nameof(ProviderCommandAppSettingActivity), (key, value));

        internal static string GetTaskHubNameSanitized(this IDurableClient client)
        {
            const int MaxTaskHubNameSize = 45;
            const int MinTaskHubNameSize = 3;
            const string TaskHubPadding = "Hub";

            if (client is null)
                throw new ArgumentNullException(nameof(client));

            var validHubNameCharacters = client.TaskHubName
                    .ToCharArray()
                    .Where(char.IsLetterOrDigit);

            if (!validHubNameCharacters.Any())
                return "DefaultTaskHub";

            if (char.IsNumber(validHubNameCharacters.First()))
            {
                // Azure Table storage requires that the task hub does not start
                // with a number. If it does, prepend "t" to the beginning.

                validHubNameCharacters = validHubNameCharacters.ToList();
                ((List<char>)validHubNameCharacters).Insert(0, 't');
            }

            var sanitizedHubName = new string(validHubNameCharacters
                .Take(MaxTaskHubNameSize)
                .ToArray());

            if (sanitizedHubName.Length < MinTaskHubNameSize)
                sanitizedHubName += TaskHubPadding;

            return sanitizedHubName;
        }
    }
}
