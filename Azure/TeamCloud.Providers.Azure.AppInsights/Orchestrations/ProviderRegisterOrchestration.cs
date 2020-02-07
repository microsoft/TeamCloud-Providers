/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.AppInsights.Activities;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
{
    public static class ProviderRegisterOrchestration
    {
        [FunctionName(nameof(ProviderRegisterOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProviderRegisterCommand>();

            var providerRegistraion = await functionContext
                .CallActivityAsync<ProviderRegistration>(nameof(ProviderRegisterActivity), command)
                .ConfigureAwait(true);

            var commandResult = command.CreateResult();
            commandResult.Result = providerRegistraion;

            functionContext.SetOutput(commandResult);
        }
    }
}
