/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.Orchestrations;

namespace TeamCloud.Providers.Core
{
    public static class TeamCloudProvidersAzureExtensions
    {
        public static Task<IReadOnlyDictionary<string, object>> GetDeploymentOutputAsync(this IDurableOrchestrationContext functionContext, string deploymentResourceId)
            => functionContext.CallSubOrchestratorWithRetryAsync<IReadOnlyDictionary<string, object>>(nameof(AzureDeploymentOrchestration), (default(string), default(object), deploymentResourceId));

        public static Task<IReadOnlyDictionary<string, object>> GetDeploymentOutputAsync(this IDurableOrchestrationContext functionContext, string deploymentActivityName, object deploymentActivityInput = default)
            => functionContext.CallSubOrchestratorWithRetryAsync<IReadOnlyDictionary<string, object>>(nameof(AzureDeploymentOrchestration), (deploymentActivityName, deploymentActivityInput, default(string)));

    }
}
