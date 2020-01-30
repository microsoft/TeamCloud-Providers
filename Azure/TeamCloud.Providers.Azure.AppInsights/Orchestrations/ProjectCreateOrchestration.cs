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
using TeamCloud.Providers.Azure.AppInsights.Activities;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
{
    public static class ProjectCreateOrchestration
    {
        [FunctionName(nameof(ProjectCreateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var providerCommand = functionContext.GetInput<ProviderCommand>();

            var providerVariables = await functionContext
                .CallActivityAsync<Dictionary<string, string>>(nameof(ProjectCreateActivity), providerCommand.Command)
                .ConfigureAwait(true);

            functionContext.SetOutput(providerVariables);

            functionContext.StartNewOrchestration(nameof(SendCommandResultOrchestration), providerCommand);
        }
    }
}
