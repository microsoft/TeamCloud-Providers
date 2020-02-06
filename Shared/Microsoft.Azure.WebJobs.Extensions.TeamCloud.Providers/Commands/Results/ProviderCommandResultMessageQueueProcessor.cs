/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Results
{
    public class ProviderCommandResultMessageQueueProcessor
    {
        [FunctionName(nameof(ProviderCommandResultMessageQueueProcessor))]
        public async Task Run(
            [QueueTrigger(Constants.Queues.ProviderCommandResults)] ProviderCommandResultMessageQueueItem providerCommandResultMessageQueueItem,
            ILogger log)
        {
            if (!string.IsNullOrEmpty(providerCommandResultMessageQueueItem.PayloadUrl))
            {
                providerCommandResultMessageQueueItem = await providerCommandResultMessageQueueItem.PayloadUrl
                    .GetJsonAsync<ProviderCommandResultMessageQueueItem>()
                    .ConfigureAwait(false);
            }

            _ = await providerCommandResultMessageQueueItem.CallbackUrl
                .PostJsonAsync(providerCommandResultMessageQueueItem.Payload)
                .ConfigureAwait(false);
        }
    }
}
