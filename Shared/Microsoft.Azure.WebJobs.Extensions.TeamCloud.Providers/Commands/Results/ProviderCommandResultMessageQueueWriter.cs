/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Results
{
    public class ProviderCommandResultMessageQueueWriter
    {
        private readonly IProviderCommandResultMessageQueueClient commandResultQueue;

        public ProviderCommandResultMessageQueueWriter(IProviderCommandResultMessageQueueClient commandResultQueue)
        {
            this.commandResultQueue = commandResultQueue ?? throw new System.ArgumentNullException(nameof(commandResultQueue));
        }

        [FunctionName(nameof(ProviderCommandResultMessageQueueWriter))]
        public Task Run(
            [ActivityTrigger] (ProviderCommandMessage, ProviderCommandResultMessage) commandMessageAndResult)
            => commandResultQueue.EnqueueAsync(new ProviderCommandResultMessageQueueItem()
            {
                CallbackUrl = commandMessageAndResult.Item1.CallbackUrl,
                Payload = commandMessageAndResult.Item2
            });
    }
}
