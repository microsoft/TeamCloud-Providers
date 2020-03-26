/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.AppInsights.Activities;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
{
    public static class ProjectCreateOrchestration
    {
        [FunctionName(nameof(ProjectCreateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProviderProjectCreateCommand>();
            var commandResult = command.CreateResult();

            var properties = await functionContext
                .CallActivityWithRetryAsync<Dictionary<string, string>>(nameof(ProjectCreateActivity), command.Payload)
                .ConfigureAwait(true);

            if (properties.TryGetValue("resourceId", out string resourceId))
            {
                await functionContext
                    .CallActivityWithRetryAsync(nameof(ProjectUsersActivity), (command.Payload, resourceId))
                    .ConfigureAwait(true);
            }

            commandResult.Result = new ProviderOutput
            {
                Properties = properties
            };

            functionContext.SetOutput(commandResult);
        }
    }
}
