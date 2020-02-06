/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Results
{
    public interface IProviderCommandResultMessageQueueClient
    {
        Task EnqueueAsync(ProviderCommandResultMessageQueueItem providerCommandResultMessageQueueItem);
    }

    public class ProviderCommandResultMessageQueueClient : IProviderCommandResultMessageQueueClient
    {
        private readonly Lazy<CloudQueue> cloudQueueInstance;

        private readonly Lazy<CloudBlobContainer> cloudContainerInstance;

        public ProviderCommandResultMessageQueueClient(IConfiguration configuration)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            cloudQueueInstance = new Lazy<CloudQueue>(()
                => CloudStorageAccount.Parse(configuration.ConnectionString).CreateCloudQueueClient().GetQueueReference(Constants.Queues.ProviderCommandResults));

            cloudContainerInstance = new Lazy<CloudBlobContainer>(()
                => CloudStorageAccount.Parse(configuration.ConnectionString).CreateCloudBlobClient().GetContainerReference(Constants.Containers.ProviderCommandResults));
        }

        private async Task<CloudQueue> GetProviderCommandResultQueueAsync()
        {
            if (!cloudQueueInstance.IsValueCreated)
            {
                await cloudQueueInstance.Value
                    .CreateIfNotExistsAsync()
                    .ConfigureAwait(false);
            }

            return cloudQueueInstance.Value;
        }

        private async Task<CloudBlobContainer> GetProviderCommandResultContainerAsync()
        {
            if (!cloudContainerInstance.IsValueCreated)
            {
                await cloudContainerInstance.Value
                    .CreateIfNotExistsAsync()
                    .ConfigureAwait(false);
            }

            return cloudContainerInstance.Value;
        }

        private async Task<string> GetProviderCommandResultContainerSasTokenAsync()
        {
            var container = await GetProviderCommandResultContainerAsync()
                .ConfigureAwait(false);

            var adHocPolicy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read
            };

            return container.GetSharedAccessSignature(adHocPolicy, null);
        }

        public async Task EnqueueAsync(ProviderCommandResultMessageQueueItem providerCommandResultMessageQueueItem)
        {
            if (providerCommandResultMessageQueueItem is null)
                throw new ArgumentNullException(nameof(providerCommandResultMessageQueueItem));

            var queueMessage = providerCommandResultMessageQueueItem
                .ToCloudQueueMessage();

            if (queueMessage.AsString.Length > CloudQueueMessage.MaxMessageSize)
            {
                var providerCommandResultMessageQueueItemRef = new ProviderCommandResultMessageQueueItem()
                {
                    CallbackUrl = providerCommandResultMessageQueueItem.CallbackUrl,
                    PayloadUrl = await PersistAsync(providerCommandResultMessageQueueItem).ConfigureAwait(false)
                };

                queueMessage = providerCommandResultMessageQueueItemRef
                    .ToCloudQueueMessage();
            }

            var queue = await GetProviderCommandResultQueueAsync()
                .ConfigureAwait(false);

            await queue
                .AddMessageAsync(queueMessage)
                .ConfigureAwait(false);
        }

        private async Task<string> PersistAsync(ProviderCommandResultMessageQueueItem providerCommandResultMessageQueueItem)
        {
            if (providerCommandResultMessageQueueItem is null)
                throw new ArgumentNullException(nameof(providerCommandResultMessageQueueItem));

            var container = await GetProviderCommandResultContainerAsync()
                .ConfigureAwait(false);

            var blob = container
                .GetBlockBlobReference($"{providerCommandResultMessageQueueItem.Payload.CommandId ?? Guid.NewGuid()}.json");

            await blob
                .UploadTextAsync(providerCommandResultMessageQueueItem.ToString())
                .ConfigureAwait(false);

            var token = await GetProviderCommandResultContainerSasTokenAsync()
                .ConfigureAwait(false);

            return $"{blob.Uri.AbsoluteUri}{token}";
        }


    }
}
