/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    internal static class ProviderAuthorizedExtensions
    {
        internal static Task EnsureAuthorizedAsync(this IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            return functionContext.CallActivityAsync(nameof(ProviderAuthorizedActivity), null);
        }
    }
}
