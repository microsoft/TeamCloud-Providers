/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public interface IAuthenticationService
    {
        Task<string> GetTokenAsync();

        Task<string> GetOrganizationUrlAsync();

        Task<string> GetOrganizationNameAsync();

        Task<string> GetServiceUrlAsync(ServiceEndpoint serviceEndpoint);

        Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default) where T : VssHttpClientBase;

        Task<bool> IsAuthorizedAsync();
    }
}
