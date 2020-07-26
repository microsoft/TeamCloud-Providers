/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Orchestration;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
{
    internal static class OperationExtensions
    {
        internal static Task CallOperationAsync(this IDurableOrchestrationContext functionContext, string operationActivityName, object operationActivityInput = default)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            if (string.IsNullOrEmpty(operationActivityName))
                throw new ArgumentException("message", nameof(operationActivityName));

            return functionContext
                .CallSubOrchestratorWithRetryAsync(nameof(OperationOrchestration), (operationActivityName, operationActivityInput, default(string)));
        }
    }
}
