/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public static class SendCommandResultActivity
    {
        [FunctionName(nameof(SendCommandResultActivity))]
        public static async Task<ICommandResult> RunOrchestration(
            [ActivityTrigger] string instanceId,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            if (instanceId is null)
                throw new ArgumentNullException(nameof(instanceId));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var status = await durableClient
               .GetStatusAsync(instanceId)
               .ConfigureAwait(false);

            ICommandResult result = null;

            if (status.IsFinalRuntimeStatus())
            {
                result = status.GetResult();
            }

            return result;
        }
    }
}
