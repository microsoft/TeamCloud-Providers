/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Providers.Core.Configuration;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Core.Activities
{
    public sealed class ProviderCommandResultAugmentActivity
    {
        private readonly IOrchestrationConfiguration orchestrationConfiguration;

        public ProviderCommandResultAugmentActivity(IOrchestrationConfiguration orchestrationConfiguration)
        {
            this.orchestrationConfiguration = orchestrationConfiguration ?? throw new ArgumentNullException(nameof(orchestrationConfiguration));
        }

        [FunctionName(nameof(ProviderCommandResultAugmentActivity))]
        public async Task<ICommandResult> RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var (commandResult, instanceId) = functionContext.GetInput<(ICommandResult, string)>();

            try
            {
                var commandStatus = await durableClient
                    .GetStatusAsync(instanceId)
                    .ConfigureAwait(false);

                if (commandStatus != null)
                {
                    commandResult = commandResult
                        .ApplyStatus(commandStatus);
                }

                if (commandResult is ProviderRegisterCommandResult providerRegisterCommandResult && providerRegisterCommandResult.Result != null)
                {
                    providerRegisterCommandResult.Result
                        .EventSubscriptions = orchestrationConfiguration.Subscriptions;
                }
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableExc))
            {
                log.LogError(exc, $"Activity '{nameof(ProviderCommandResultAugmentActivity)}' failed: {exc.Message}");

                throw serializableExc;
            }
            finally
            {
                log.LogInformation($"Augmented command result ({commandResult.CommandId}): {JsonConvert.SerializeObject(commandResult)}");
            }

            return commandResult;
        }
    }
}
