﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Providers.Azure.DevOps.Activities;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
{
    public static class ProjectCreateOrchestration
    {
        [FunctionName(nameof(ProjectCreateOrchestration))]
        public static async Task<ProjectCreateActivity.Result> RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var request = functionContext.GetInput<OrchestrationRequest>();

            var commandResult = await functionContext
                .CallActivityAsync<ProjectCreateActivity.Result>(nameof(ProjectCreateActivity), request.Command)
                .ConfigureAwait(true);

            if (!string.IsNullOrEmpty(request.CallbackUrl))
            {
                functionContext.StartNewOrchestration(nameof(SendCommandResultOrchestration), new SendCommandResultOrchestration.Request
                {
                    InstanceId = functionContext.InstanceId,
                    CallbackUrl = request.CallbackUrl
                });
            }

            return commandResult;
        }
    }
}