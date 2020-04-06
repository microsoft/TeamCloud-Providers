/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.AppInsights;
using TeamCloud.Providers.Core.Activities;
using TeamCloud.Providers.Core.Configuration;

namespace TeamCloud.Providers.Core
{
    public static class Extensions
    {
        public static ICommandResult ApplyStatus(this ICommandResult commandResult, DurableOrchestrationStatus orchestrationStatus)
        {
            if (commandResult is null)
                throw new ArgumentNullException(nameof(commandResult));

            if (orchestrationStatus is null)
                throw new ArgumentNullException(nameof(orchestrationStatus));

            commandResult.CreatedTime = orchestrationStatus.CreatedTime;
            commandResult.LastUpdatedTime = orchestrationStatus.LastUpdatedTime;
            commandResult.RuntimeStatus = (CommandRuntimeStatus)orchestrationStatus.RuntimeStatus;
            commandResult.CustomStatus = orchestrationStatus.CustomStatus?.ToString();

            return commandResult;
        }

        public static IServiceCollection AddTeamCloudCommandOrchestration(this IServiceCollection services, Action<OrchestrationConfiguration> config)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            if (config is null)
                throw new ArgumentNullException(nameof(config));

            services
                .TryAddSingleton<IOrchestrationConfiguration>(new OrchestrationConfiguration());

            var configuration = services
                .BuildServiceProvider()
                .GetRequiredService<IOrchestrationConfiguration>();

            if (configuration is OrchestrationConfiguration configurationTyped)
                config(configurationTyped);

            return services;
        }

        public static async Task<ICommandResult> GetCommandResultAsync(this IDurableClient durableClient, ICommand command)
        {
            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var commandStatus = await durableClient
                .GetStatusAsync(CommandHandler.GetCommandOrchestrationInstanceId(command))
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
                    .GetStatusAsync(CommandHandler.GetCommandMessageOrchestrationInstanceId(command))
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

        public static IDictionary<Guid, IEnumerable<Guid>> ToRoleAssignments(this IList<User> users, Func<string, Guid> roleIdCallback)
            => users.ToDictionary(user => user.Id, user => Enumerable.Repeat(roleIdCallback(user.Role), 1));

        public static Task SetInstrumentationKeyAsync(this IDurableOrchestrationContext functionContext, Guid instrumentationKey)
            => instrumentationKey.ToString().Equals(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"), StringComparison.OrdinalIgnoreCase)
            ? Task.CompletedTask : functionContext.SetAppSettingAsync("APPINSIGHTS_INSTRUMENTATIONKEY", instrumentationKey.ToString());

        public static Task SetAppSettingAsync(this IDurableOrchestrationContext functionContext, string key, string value = default)
            => functionContext.CallActivityWithRetryAsync(nameof(ProviderCommandAppSettingActivity), (key, value));
    }
}
