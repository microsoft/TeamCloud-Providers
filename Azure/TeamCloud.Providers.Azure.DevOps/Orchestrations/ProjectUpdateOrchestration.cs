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
using TeamCloud.Providers.Azure.DevOps.Activities;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
{
    public static class ProjectUpdateOrchestration
    {
        [FunctionName(nameof(ProjectUpdateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var providerCommand = functionContext.GetInput<ProviderCommand>();

            var providerVariables = await functionContext
                .CallActivityAsync<Dictionary<string, string>>(nameof(ProjectUpdateActivity), providerCommand.Command)
                .ConfigureAwait(true);

            functionContext.SetOutput(providerVariables);

            functionContext.StartNewOrchestration(nameof(SendCommandResultOrchestration), providerCommand);
        }
    }
}
