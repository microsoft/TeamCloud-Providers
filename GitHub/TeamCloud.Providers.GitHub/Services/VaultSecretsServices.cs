/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration;

namespace TeamCloud.Providers.GitHub.Services
{
    public sealed class VaultSecretsServices : ISecretsService
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
}
