/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Core.Commands.Results
{
    public sealed class ProviderCommandResultQueueWriter
    {
        private readonly IProviderCommandResultQueueClient commandResultQueue;

        public ProviderCommandResultQueueWriter(IProviderCommandResultQueueClient commandResultQueue)
        {
            this.commandResultQueue = commandResultQueue ?? throw new System.ArgumentNullException(nameof(commandResultQueue));
        }

        [FunctionName(nameof(ProviderCommandResultQueueWriter))]
        public Task Run(
            [ActivityTrigger] (ProviderCommandMessage, ICommandResult) commandMessageAndResult)
            => commandResultQueue.EnqueueAsync(new ProviderCommandResultQueueItem()
            {
                CallbackUrl = commandMessageAndResult.Item1.CallbackUrl,
                Payload = commandMessageAndResult.Item2
            });
    }
}
