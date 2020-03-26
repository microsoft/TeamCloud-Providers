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
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Activities;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
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

            var providerRegistraion = await functionContext
                .CallActivityWithRetryAsync<ProviderRegistration>(nameof(ProviderRegisterActivity), command)
                .ConfigureAwait(true);

            var commandResult = command.CreateResult();
            commandResult.Result = providerRegistraion;

            functionContext.SetOutput(commandResult);
        }
    }
}
