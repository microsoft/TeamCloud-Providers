/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public sealed class StorageSecretsService : ISecretsService
    {
        private readonly CloudBlobClient cloudBlobClient;
        private readonly Lazy<CloudBlobContainer> cloudBlobContainerInstance;

        public StorageSecretsService(IConfiguration configuration)
            : this((configuration ?? throw new ArgumentNullException(nameof(configuration))).GetValue<string>("AzureWebJobsStorage") ?? throw new ArgumentException("Application setting 'AzureWebJobsStorage' could not be found"))
        { }

        public StorageSecretsService(string storageConnectionString)
        {
            cloudBlobClient = StorageAccount
                .NewFromConnectionString(storageConnectionString)
                .CreateCloudBlobClient();

            cloudBlobContainerInstance = new Lazy<CloudBlobContainer>(() => cloudBlobClient
                .GetContainerReference("secrets"));
        }

        private async Task<CloudBlockBlob> GetSecretsBlobAsync(string key)
        {
            if (!cloudBlobContainerInstance.IsValueCreated)
                _ = await cloudBlobContainerInstance.Value
                    .CreateIfNotExistsAsync()
                    .ConfigureAwait(false);

            var path = $"{Assembly.GetExecutingAssembly().GetName().Name}/{key}.json";

            return cloudBlobContainerInstance.Value
                .GetBlockBlobReference(path);
        }

        public async Task<string> GetSecretAsync(string key)
        {
            var blob = await GetSecretsBlobAsync(key)
                .ConfigureAwait(false);

            if (await blob.ExistsAsync().ConfigureAwait(false))
            {
                return await blob
                    .DownloadTextAsync()
                    .ConfigureAwait(false);
            }

            return null;
        }

        public async Task<string> SetSecretAsync(string key, string value)
        {
            var blob = await GetSecretsBlobAsync(key)
                .ConfigureAwait(false);

            if (value is null)
            {
                await blob
                    .DeleteIfExistsAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                await blob
                    .UploadTextAsync(value)
                    .ConfigureAwait(false);
            }

            return value;
        }
    }
}
