/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.AppInsights.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
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

            var project = functionContext.GetInput<Project>();
            var projectLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            using (log.BeginProjectScope(project))
            {
                try
                {
                    var resources = await functionContext
                        .CallActivityWithRetryAsync<IEnumerable<string>>(nameof(ProjectResourceListActivity), project)
                        .ConfigureAwait(true);

                    var tasks = new List<Task>();

                    tasks.AddRange(resources.Select(resource => functionContext.CallActivityWithRetryAsync(nameof(ProjectResourceRolesActivity), (project, resource))));
                    tasks.AddRange(resources.Select(resource => functionContext.CallActivityWithRetryAsync(nameof(ProjectResourceLinksActivity), (project, resource))));
                    tasks.AddRange(resources.Select(resource => functionContext.CallActivityWithRetryAsync(nameof(ProjectResourceTagsActivity), (project, resource))));

                    await Task
                        .WhenAll(tasks)
                        .ConfigureAwait(true);

                }
                catch (Exception exc) when (!exc.IsSerializable())
                {
                    log.LogError(exc, $"Orchestration '{nameof(ProjectSyncOrchestration)}' failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
