/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TeamCloud.Providers.Core.Commands.Results
{
    public sealed class ProviderCommandResultQueueProcessor
    {
        [FunctionName(nameof(ProviderCommandResultQueueProcessor))]
        public async Task Run(
            [QueueTrigger(Constants.Queues.ProviderCommandResults)] ProviderCommandResultQueueItem providerCommandResultMessageQueueItem,
            ILogger log)
        {
            if (!string.IsNullOrEmpty(providerCommandResultMessageQueueItem.PayloadUrl))
            {
                providerCommandResultMessageQueueItem = await providerCommandResultMessageQueueItem.PayloadUrl
                    .GetJsonAsync<ProviderCommandResultQueueItem>()
                    .ConfigureAwait(false);
            }

            _ = await providerCommandResultMessageQueueItem.CallbackUrl
                .PostJsonAsync(providerCommandResultMessageQueueItem.Payload)
                .ConfigureAwait(false);
        }
    }
}
