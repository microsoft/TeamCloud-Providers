/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Flurl.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using TeamCloud.Http;
using TeamCloud.Orchestration.Threading;
using TeamCloud.Providers.Azure.DevOps.Data;
using TeamCloud.Providers.Azure.DevOps.Handlers;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public sealed class AuthenticationService : IAuthenticationService, IAuthenticationSetup
    {
        private readonly ISecretsService secretsService;
        private readonly IDistributedLockManager distributedLockManager;
        private readonly ILogger<AuthenticationService> logger;

        public AuthenticationService(ISecretsService secretsService, IDistributedLockManager distributedLockManager, ILogger<AuthenticationService> logger = null)
        {
            this.secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            this.distributedLockManager = distributedLockManager ?? throw new ArgumentNullException(nameof(distributedLockManager));
            this.logger = logger ?? NullLogger<AuthenticationService>.Instance;
        }

        private async Task<AuthorizationToken> GetValidAuthorizationTokenAsync()
        {
            var token = await ((IAuthenticationSetup)this)
                .GetAsync()
                .ConfigureAwait(false);

            if (IsTokenExpired(token))
            {
                IDistributedLock distributedLock;

                try
                {
                    distributedLock = await distributedLockManager
                        .AcquireLockAsync(nameof(AuthorizationToken), this.GetType().Name)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException exc)
                {
                    logger.LogWarning(exc, $"Failed to acquire distributed lock to refresh authorization token: {exc.Message}");

                    return await ((IAuthenticationSetup)this)
                        .GetAsync()
                        .ConfigureAwait(false);
                }

                logger.LogInformation($"Acquired lock {distributedLock.LockId}");

                try
                {
                    token = await ((IAuthenticationSetup)this)
                        .GetAsync()
                        .ConfigureAwait(false);

                    if (IsTokenExpired(token))
                    {
                        logger.LogInformation($"Current authorization token expires {token?.AccessTokenExpires}");

                        var refreshedToken = await AuthorizationHandler
                            .RefreshAsync(token)
                            .ConfigureAwait(false);

                        if (refreshedToken != null)
                        {
                            logger.LogInformation($"Refreshed authorization token expires {refreshedToken?.AccessTokenExpires}");

                            await ((IAuthenticationSetup)this)
                                .SetAsync(refreshedToken)
                                .ConfigureAwait(false);

                            token = refreshedToken;
                        }
                    }
                }
                finally
                {
                    await distributedLockManager
                        .ReleaseLockAsync(distributedLock, CancellationToken.None)
                        .ConfigureAwait(false);

                    logger.LogInformation($"Released lock {distributedLock.LockId}");
                }
            }

            return token;

            static bool IsTokenExpired(AuthorizationToken authorizationToken)
                => authorizationToken != null && authorizationToken.AccessTokenExpires.GetValueOrDefault(DateTime.UtcNow).AddMinutes(-30) < DateTime.UtcNow;
        }

        public async Task<string> GetTokenAsync()
        {
            var authorizationToken = await GetValidAuthorizationTokenAsync()
                .ConfigureAwait(false);

            return authorizationToken?.AccessToken;
        }

        public async Task<string> GetSubjectDescriptorAsync()
        {
            var token = await GetTokenAsync()
                .ConfigureAwait(false);

            var org = await GetOrganizationNameAsync()
                .ConfigureAwait(false);

            var json = await $"https://{org}.vssps.visualstudio.com/_apis/connectionData"
                .WithOAuthBearerToken(token)
                .GetJObjectAsync()
                .ConfigureAwait(false);

            return json.SelectTokens("$..subjectDescriptor")
                .FirstOrDefault()?
                .ToString();
        }

        public async Task<string> GetServiceUrlAsync(ServiceEndpoint serviceEndpoint)
        {
            var organization = await GetOrganizationNameAsync()
                .ConfigureAwait(false);

            return serviceEndpoint.ToUrl(organization);
        }

        public async Task<string> GetOrganizationUrlAsync()
        {
            var authorizationToken = await GetValidAuthorizationTokenAsync()
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
            var authorizationToken = await GetValidAuthorizationTokenAsync()
                .ConfigureAwait(false);

            if (authorizationToken is null)
                return false;

            var result = await new AuthorizationTokenValidator()
                .ValidateAsync(authorizationToken)
                .ConfigureAwait(false);

            return result.IsValid;
        }

        async Task<AuthorizationToken> IAuthenticationSetup.SetAsync(AuthorizationToken authorizationToken)
        {
            if (authorizationToken != null)
            {
                var result = await new AuthorizationTokenValidator()
                    .ValidateAsync(authorizationToken)
                    .ConfigureAwait(false);

                if (!result.IsValid)
                    throw new ValidationException(result.Errors);
            }

            var authorizationSecret = authorizationToken is null
                ? null
                : JsonConvert.SerializeObject(authorizationToken);

            authorizationSecret = await secretsService
                .SetSecretAsync(nameof(AuthenticationService), authorizationSecret)
                .ConfigureAwait(false);

            return string.IsNullOrEmpty(authorizationSecret)
                ? null
                : JsonConvert.DeserializeObject<AuthorizationToken>(authorizationSecret);
        }

        async Task<AuthorizationToken> IAuthenticationSetup.GetAsync()
        {
            var secret = await secretsService
                .GetSecretAsync(nameof(AuthenticationService))
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(secret))
                return null;

            return JsonConvert.DeserializeObject<AuthorizationToken>(secret);
        }

        public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default)
            where T : VssHttpClientBase
        {
            var token = await GetValidAuthorizationTokenAsync()
                .ConfigureAwait(false);

            if (token is null)
                return null;

            var connectionUri = new Uri(token.Organization);
            var connectionCred = new VssOAuthAccessTokenCredential(token.AccessToken);
            using var connection = new VssConnection(connectionUri, connectionCred);

            return await connection
                .GetClientAsync<T>(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
