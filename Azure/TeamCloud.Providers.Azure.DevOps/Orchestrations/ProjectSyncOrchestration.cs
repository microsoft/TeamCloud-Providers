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
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
{
    public static class ProjectSyncOrchestration
    {
        [FunctionName(nameof(ProjectSyncOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var project = functionContext
                .GetInput<Project>();

            try
            {
                var tasks = new List<Task>()
                {
                    functionContext.CallActivityWithRetryAsync(nameof(SynchronizeLinksActivity), project),
                    functionContext.CallActivityWithRetryAsync(nameof(SynchronizeServiceConnectionsActivity), project),
                    functionContext.CallActivityWithRetryAsync(nameof(SynchronizeUsersActivity), project)
                };

                await Task
                    .WhenAll(tasks)
                    .ConfigureAwait(true);
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Synchronizing project {project.Id} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
