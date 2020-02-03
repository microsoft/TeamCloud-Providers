/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.AppInsights.Activities;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
{
    public static class SendCommandResultOrchestration
    {
        [FunctionName(nameof(SendCommandResultOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var providerCommandMessage = functionContext.GetInput<ProviderCommandMessage>();

            // this orchestration is being called from the same orchestration
            // that we're sending the result for.  thus the calling orchestration
            // needs to be finished.  to ensure this we wait for a second before
            // calling the activity, and try again if the activity returns false

            await functionContext
                .CreateTimer(functionContext.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None)
                .ConfigureAwait(true);

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 10);

            var success = await functionContext
                .CallActivityWithRetryAsync<bool>(nameof(SendCommandResultActivity), retryOptions, providerCommandMessage)
                .ConfigureAwait(true);

            // calling orchestraiton isn't in a final state (finished)
            if (!success)
            {
                functionContext.ContinueAsNew(providerCommandMessage);
            }
        }
    }
}
