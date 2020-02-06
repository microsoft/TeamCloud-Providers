/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Results
{
    public class ProviderCommandResultMessageQueueItem
    {
        public string CallbackUrl { get; set; }

        public ProviderCommandResultMessage Payload { get; set; }

        public string PayloadUrl { get; set; }

        public CloudQueueMessage ToCloudQueueMessage()
            => new CloudQueueMessage(ToString());

        public override string ToString()
            => JsonConvert.SerializeObject(this);
    }
}
