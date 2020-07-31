/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using TeamCloud.Providers.Azure.DevOps.Data;
using TeamCloud.Providers.Azure.DevOps.Handlers;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public interface IAuthenticationService
    {
        Task<string> GetTokenAsync();

        Task<string> GetOrganizationUrlAsync();

        Task<string> GetOrganizationNameAsync();

        Task<string> GetServiceUrlAsync(ServiceEndpoint serviceEndpoint);

        //Task<VssConnection> GetConnectionAsync();

        Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default) where T : VssHttpClientBase;

        Task<bool> IsAuthorizedAsync();
    }

    public interface IAuthenticationSetup
    {
        Task SetupAsync(AuthorizationToken authorizationToken);
    }

    internal class AuthenticationService : IAuthenticationService, IAuthenticationSetup
    {
        private readonly ISecretsService secretsService;

        public AuthenticationService(ISecretsService secretsService)
        {
            this.secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        private async Task<AuthorizationToken> GetAuthorizationTokenAsync()
        {
            var secret = await secretsService
                .GetSecretAsync(nameof(AuthenticationService))
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(secret))
                return null;

            var token = JsonConvert.DeserializeObject<AuthorizationToken>(secret);

            if (token.AccessTokenExpires.GetValueOrDefault(DateTime.UtcNow).AddMinutes(-1) < DateTime.UtcNow)
            {
                token = await AuthorizationHandler
                    .RefreshAsync(token)
                    .ConfigureAwait(false);

                await ((IAuthenticationSetup)this)
                    .SetupAsync(token)
                    .ConfigureAwait(false);
            }

            return token;
        }

        //public async Task<VssConnection> GetConnectionAsync()
        //{
        //    var token = await GetAuthorizationTokenAsync()
        //        .ConfigureAwait(false);

        //    if (token is null)
        //        return null;

        //    var connectionUri = new Uri(token.Organization);
        //    var connectionCred = new VssOAuthAccessTokenCredential(token.AccessToken);

        //    return new VssConnection(connectionUri, connectionCred);
        //}

        public async Task<string> GetTokenAsync()
        {
            var authorizationToken = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            return authorizationToken?.AccessToken;
        }

        public async Task<string> GetServiceUrlAsync(ServiceEndpoint serviceEndpoint)
        {
            var organization = await GetOrganizationNameAsync()
                .ConfigureAwait(false);

            return serviceEndpoint switch
            {
                ServiceEndpoint.ApiRoot => $"https://dev.azure.com/{organization}/_apis",
                ServiceEndpoint.UserEntitlements => $"https://vsaex.dev.azure.com/{organization}/_apis/userentitlements",
                _ => throw new NotSupportedException($"Service endpoint of type '{serviceEndpoint}' is not supported"),
            };
        }

        public async Task<string> GetOrganizationUrlAsync()
        {
            var authorizationToken = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            return authorizationToken?.Organization;
        }

        public async Task<string> GetOrganizationNameAsync()
        {
            string organizationName = null;

            var organizationUrl = await GetOrganizationUrlAsync()
                .ConfigureAwait(false);

            if (Uri.TryCreate(organizationUrl, UriKind.Absolute, out Uri url))
            {
                if (url.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
                {
                    organizationName = url.AbsolutePath
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .First();
                }
                else if (url.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase))
                {
                    organizationName = url.Host
                        .Split('.', StringSplitOptions.RemoveEmptyEntries)
                        .First();
                }
            }

            return organizationName;
        }

        public async Task<bool> IsAuthorizedAsync()
        {
            var authorizationToken = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            if (authorizationToken is null)
                return false;

            var result = await new AuthorizationValidator()
                .ValidateAsync(authorizationToken)
                .ConfigureAwait(false);

            return result.IsValid;
        }

        async Task IAuthenticationSetup.SetupAsync(AuthorizationToken authorizationToken)
        {
            if (authorizationToken != null)
            {
                var result = await new AuthorizationValidator()
                    .ValidateAsync(authorizationToken)
                    .ConfigureAwait(false);

                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }

            var authorizationSecret = authorizationToken is null
                ? null
                : JsonConvert.SerializeObject(authorizationToken);

            _ = await secretsService
                .SetSecretAsync(nameof(AuthenticationService), authorizationSecret)
                .ConfigureAwait(false);
        }

        public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default)
            where T : VssHttpClientBase
        {
            var token = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            if (token is null)
                return null;

            var connectionUri = new Uri(token.Organization);
            var connectionCred = new VssOAuthAccessTokenCredential(token.AccessToken);
            var connection = new VssConnection(connectionUri, connectionCred);

            return await connection
                .GetClientAsync<T>()
                .ConfigureAwait(false);
        }
    }
}
