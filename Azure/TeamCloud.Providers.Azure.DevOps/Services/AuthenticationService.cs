/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
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

        Task<string> GetUrlAsync();

        Task<VssConnection> GetConnectionAsync();

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

            if (token.AccessTokenExpires.GetValueOrDefault(DateTime.UtcNow) > DateTime.UtcNow.AddMinutes(-5))
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

        public async Task<VssConnection> GetConnectionAsync()
        {
            var token = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            if (token is null)
                return null;

            var connectionUri = new Uri(token.Organization);
            var connectionCred = new VssOAuthAccessTokenCredential(token.AccessToken);

            return new VssConnection(connectionUri, connectionCred);
        }

        public async Task<string> GetTokenAsync()
        {
            var authorizationToken = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            return authorizationToken?.AccessToken;
        }

        public async Task<string> GetUrlAsync()
        {
            var authorizationToken = await GetAuthorizationTokenAsync()
                .ConfigureAwait(false);

            return authorizationToken?.Organization;
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
    }
}
