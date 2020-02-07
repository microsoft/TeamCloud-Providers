/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Core.Commands.Results
{
    public sealed class ProviderCommandResultQueueItem
    {
        public string CallbackUrl { get; set; }

        public string PayloadUrl { get; set; }

        public ICommandResult Payload { get; set; }

        public CloudQueueMessage ToCloudQueueMessage()
            => new CloudQueueMessage(ToString());

        public override string ToString()
            => JsonConvert.SerializeObject(this);
    }
}
