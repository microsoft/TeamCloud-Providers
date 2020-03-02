/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Core.Commands.Orchestrations;

namespace TeamCloud.Providers.Core
{
    public static class Extensions
    {
        private static readonly int[] FinalRuntimeStatus = new int[]
        {
            (int) OrchestrationRuntimeStatus.Canceled,
            (int) OrchestrationRuntimeStatus.Completed,
            (int) OrchestrationRuntimeStatus.Terminated
        };

        public static bool IsFinalRuntimeStatus(this DurableOrchestrationStatus orchestrationStatus)
            => FinalRuntimeStatus.Contains((int)orchestrationStatus.RuntimeStatus);

        private static ICommandResult ApplyOrchestrationStatus(this ICommandResult commandResult, DurableOrchestrationStatus orchestrationStatus)
        {
            if (orchestrationStatus is null)
                throw new ArgumentNullException(nameof(orchestrationStatus));

            commandResult.CreatedTime = orchestrationStatus.CreatedTime;
            commandResult.LastUpdatedTime = orchestrationStatus.LastUpdatedTime;
            commandResult.RuntimeStatus = (CommandRuntimeStatus)orchestrationStatus.RuntimeStatus;
            commandResult.CustomStatus = orchestrationStatus.CustomStatus?.ToString();

            if (orchestrationStatus.IsFinalRuntimeStatus() && (orchestrationStatus.Output?.HasValues ?? false))
            {
                var orchstrationResultType = commandResult.GetType()
                    .GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandResult<>))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault();

                if (orchstrationResultType != null)
                {
                    var orchstrationResult = orchestrationStatus.Output.ToObject(orchstrationResultType);

                    commandResult.GetType().GetProperty("Result").SetValue(commandResult, orchstrationResult);
                }
            }

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

        public static async Task<ICommandResult> HandleProviderCommandMessageAsync(this IDurableClient durableClient, ProviderCommandMessage providerCommandMessage)
        {
            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            if (providerCommandMessage is null)
                throw new ArgumentNullException(nameof(providerCommandMessage));

            if (providerCommandMessage.CommandId is null)
                throw new ArgumentException("The given command message doesn't contain a command");

            var commandStatus = await durableClient
                .GetStatusAsync(providerCommandMessage.CommandId.ToString())
                .ConfigureAwait(false);

            if (commandStatus != null)
            {
                var exceptionMessage = commandStatus.IsFinalRuntimeStatus()
                    ? $"The command {providerCommandMessage.Command} was already processed."
                    : $"The command {providerCommandMessage.Command} is already in progress.";

                throw new ArgumentException(exceptionMessage);
            }

            try
            {
                _ = await durableClient
                    .StartNewAsync(nameof(ProviderCommandMessageOrchestration), providerCommandMessage)
                    .ConfigureAwait(false);

                var timeoutDuration = TimeSpan.FromSeconds(30);
                var timeout = DateTime.UtcNow.Add(timeoutDuration);

                while (DateTime.UtcNow <= timeout)
                {
                    commandStatus = await durableClient
                        .GetStatusAsync(providerCommandMessage.CommandId.ToString())
                        .ConfigureAwait(false);

                    if (commandStatus is null)
                    {
                        await Task
                            .Delay(1000)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        return providerCommandMessage.Command
                            .CreateResult()
                            .ApplyOrchestrationStatus(commandStatus);
                    }
                }

                throw new TimeoutException($"Failed to get status for command {providerCommandMessage.CommandId} within {timeoutDuration}");
            }
            catch (Exception exc)
            {
                var commandResult = providerCommandMessage.Command
                    .CreateResult()
                    .ApplyOrchestrationStatus(commandStatus);

                commandResult.Errors.Add(exc);

                return commandResult;
            }
        }
    }
}
