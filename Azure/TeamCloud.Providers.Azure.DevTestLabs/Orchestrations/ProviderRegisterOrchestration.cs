/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevTestLabs.Activities;
using TeamCloud.Providers.Core;

namespace TeamCloud.Providers.Azure.DevTestLabs.Orchestrations
{
    public static class ProviderRegisterOrchestration
    {
        [FunctionName(nameof(ProviderRegisterOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProviderRegisterCommand>();

            if (Guid.TryParse(command.Payload?.TeamCloudApplicationInsightsKey, out var instrumentationKey))
            {
                await functionContext
                    .SetInstrumentationKeyAsync(instrumentationKey)
                    .ConfigureAwait(true);
            }

            var providerRegistraion = await functionContext
                .CallActivityWithRetryAsync<ProviderRegistration>(nameof(ProviderRegisterActivity), command)
                .ConfigureAwait(true);

            var commandResult = command.CreateResult();

            commandResult.Result = providerRegistraion;

            functionContext.SetOutput(commandResult);
        }
    }
}
