/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Providers.Azure.Orchestrations;

namespace TeamCloud.Providers.Core
{
    public static class TeamCloudProvidersAzureExtensions
    {
        public static Task<IReadOnlyDictionary<string, object>> GetDeploymentOutputAsync(this IDurableOrchestrationContext functionContext, string resourceId)
            => functionContext.CallSubOrchestratorAsync<IReadOnlyDictionary<string, object>>(nameof(AzureDeploymentOutputOrchestration), resourceId);
    }
}
