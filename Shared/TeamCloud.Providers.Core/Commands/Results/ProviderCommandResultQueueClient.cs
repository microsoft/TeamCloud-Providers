/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

namespace TeamCloud.Providers.Core.Commands.Results
{
    public interface IProviderCommandResultQueueClient
    {
        Task EnqueueAsync(ProviderCommandResultQueueItem providerCommandResultMessageQueueItem);
    }

    public sealed class ProviderCommandResultQueueClient : IProviderCommandResultQueueClient
    {
        private readonly Lazy<CloudQueue> cloudQueueInstance;

        private readonly Lazy<CloudBlobContainer> cloudContainerInstance;

        public ProviderCommandResultQueueClient(IOrchestrationConfiguration configuration)
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

        public async Task EnqueueAsync(ProviderCommandResultQueueItem providerCommandResultQueueItem)
        {
            if (providerCommandResultQueueItem is null)
                throw new ArgumentNullException(nameof(providerCommandResultQueueItem));

            var queueMessage = providerCommandResultQueueItem
                .ToCloudQueueMessage();

            if (queueMessage.AsString.Length > CloudQueueMessage.MaxMessageSize)
            {
                var providerCommandResultQueueItemRef = new ProviderCommandResultQueueItem()
                {
                    CallbackUrl = providerCommandResultQueueItem.CallbackUrl,
                    PayloadUrl = await PersistAsync(providerCommandResultQueueItem).ConfigureAwait(false)
                };

                queueMessage = providerCommandResultQueueItemRef
                    .ToCloudQueueMessage();
            }

            var queue = await GetProviderCommandResultQueueAsync()
                .ConfigureAwait(false);

            await queue
                .AddMessageAsync(queueMessage)
                .ConfigureAwait(false);
        }

        private async Task<string> PersistAsync(ProviderCommandResultQueueItem providerCommandResultQueueItem)
        {
            if (providerCommandResultQueueItem is null)
                throw new ArgumentNullException(nameof(providerCommandResultQueueItem));

            var container = await GetProviderCommandResultContainerAsync()
                .ConfigureAwait(false);

            var blob = container
                .GetBlockBlobReference($"{Guid.NewGuid()}.json");

            await blob
                .UploadTextAsync(providerCommandResultQueueItem.ToString())
                .ConfigureAwait(false);

            var token = await GetProviderCommandResultContainerSasTokenAsync()
                .ConfigureAwait(false);

            return $"{blob.Uri.AbsoluteUri}{token}";
        }
    }
}
