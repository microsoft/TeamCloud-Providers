using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public interface ISecretsService
    {
        Task<string> GetSecretAsync(string key);

        Task<string> SetSecretAsync(string key, string value);
    }

    internal sealed class VaultSecretsServices : ISecretsService
    {
        private readonly IKeyVaultClient keyVaultClient;
        private readonly string keyVaultName;

        public VaultSecretsServices(IKeyVaultClient keyVaultClient, IConfiguration configuration)
            : this(keyVaultClient, configuration?.GetValue<string>("KeyVaultName"))
        { }

        public VaultSecretsServices(IKeyVaultClient keyVaultClient, string keyVaultName)
        {
            this.keyVaultClient = keyVaultClient ?? throw new ArgumentNullException(nameof(keyVaultClient));
            this.keyVaultName = keyVaultName ?? throw new ArgumentNullException(nameof(keyVaultName));
        }

        public string KeyVaultUrl { get => $"https://{keyVaultName}.vault.azure.net/"; }

        public async Task<string> GetSecretAsync(string key)
        {
            try
            {

                var secret = await keyVaultClient
                    .GetSecretAsync(KeyVaultUrl, key)
                    .ConfigureAwait(false);

                return secret.Value;
            }
            catch (KeyVaultErrorException)
            {
                return null;
            }
        }

        public async Task<string> SetSecretAsync(string key, string value)
        {
            var secret = await keyVaultClient
                .SetSecretAsync(KeyVaultUrl, key, value)
                .ConfigureAwait(false);

            return secret.Value;
        }
    }

    internal sealed class StorageSecretsService : ISecretsService
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

            return await blob
                .DownloadTextAsync()
                .ConfigureAwait(false);
        }

        public async Task<string> SetSecretAsync(string key, string value)
        {
            var blob = await GetSecretsBlobAsync(key)
                .ConfigureAwait(false);

            await blob
                .UploadTextAsync(value)
                .ConfigureAwait(false);

            return value;
        }
    }
}
