/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using TeamCloud.Providers.Azure.DevOps.Data;
using TeamCloud.Providers.Azure.DevOps.Handlers;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public interface IAuthenticationService
    {
        Task<string> GetConnectionTokenAsync();

        Task<string> GetConnectionUrlAsync();

        Task<VssConnection> GetConnectionAsync();
    }

    internal class AuthenticationService : IAuthenticationService, IAuthorizationSetup
    {
        private readonly ISecretsService secretsService;

        public AuthenticationService(ISecretsService secretsService)
        {
            this.secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        private async Task<AuthorizationToken> GetTokenAsync()
        {
            var secret = await secretsService
                .GetSecretAsync(nameof(AuthenticationService))
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(secret))
                return null;

            var token = JsonConvert.DeserializeObject<AuthorizationToken>(secret);

            if (token.AccessTokenExpires.GetValueOrDefault(DateTime.UtcNow) > DateTime.UtcNow.AddMinutes(-5))
            {
                token = await AuthorizationHandler
                    .RefreshAsync(token)
                    .ConfigureAwait(false);

                await ((IAuthorizationSetup)this)
                    .SetupAuthorizationAsync(token)
                    .ConfigureAwait(false);
            }

            return token;
        }

        public async Task<VssConnection> GetConnectionAsync()
        {
            var token = await GetTokenAsync()
                .ConfigureAwait(false);

            if (token is null)
                return null;

            var connectionUri = new Uri(token.Organization);
            var connectionCred = new VssOAuthAccessTokenCredential(token.AccessToken);

            return new VssConnection(connectionUri, connectionCred);
        }

        public async Task<string> GetConnectionTokenAsync()
        {
            var token = await GetTokenAsync()
                .ConfigureAwait(false);

            return token?.AccessToken;
        }

        public async Task<string> GetConnectionUrlAsync()
        {
            var token = await GetTokenAsync()
                .ConfigureAwait(false);

            return token?.Organization;
        }


        async Task IAuthorizationSetup.SetupAuthorizationAsync(AuthorizationToken authorizationToken)
        {
            var secret = authorizationToken is null ? null : JsonConvert.SerializeObject(authorizationToken);

            _ = await secretsService
                .SetSecretAsync(nameof(AuthenticationService), secret)
                .ConfigureAwait(false);
        }

    }
}
