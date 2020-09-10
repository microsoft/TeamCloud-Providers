/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public interface ISecretsService
    {
        Task<string> GetSecretAsync(string key);

        Task<string> SetSecretAsync(string key, string value);
    }
}
