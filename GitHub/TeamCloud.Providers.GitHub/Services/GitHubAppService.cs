/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.GitHub.Data;

namespace TeamCloud.Providers.GitHub.Services
{
    public class GitHubAppService
    {
        private GitHubClient _client;
        private GitHubAppManifest _app;

        private readonly SimpleJsonSerializer SimpleJsonSerializer = new SimpleJsonSerializer();

        private static ProductHeaderValue ProductHeader => new ProductHeaderValue(GitHubServiceConstants.ProductHeaderName, GitHubServiceConstants.ProductHeaderVersion);

        private static GitHubJwtFactory JwtTokenGenerator(GitHubAppManifest app) => new GitHubJwtFactory(
            new StringPrivateKeySource(app?.Pem ?? throw new InvalidOperationException("Must have GitHub App Pem key before initializing GitHub client")),
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = (int)app.Id, // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            });

        private readonly ISecretsService secretsService;

        public GitHubAppService(ISecretsService secretsService)
        {
            this.secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var app = await GetAppManifestAsync()
                .ConfigureAwait(false);

            return app != default;
        }

        public async Task<GitHubClient> GetAppClientAsync()
        {
            if (!(_client is null))
            {
                var token = await GetSecretAsync<AccessToken>()
                    .ConfigureAwait(false);

                if (token is null || token.ExpiresAt < DateTimeOffset.Now.AddMinutes(5))
                    _client = null;
            }

            if (_client is null)
            {
                var app = await GetAppManifestAsync()
                    .ConfigureAwait(false);

                var jwtToken = JwtTokenGenerator(app).CreateEncodedJwtToken();

                var appClient = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
                };

                var installation = await GetSecretAsync<InstallationWithSlug>()
                    .ConfigureAwait(false);

                var token = await appClient
                    .GitHubApps
                    .CreateInstallationToken(installation?.Id ?? throw new InvalidOperationException("Must have GitHub App Installation before initializing GitHub client"))
                    .ConfigureAwait(false);

                await SetSecretAsync(token)
                    .ConfigureAwait(false);

                _client = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(token.Token)
                };
            }

            return _client;
        }

        internal async Task<GitHubAppManifest> ConvertAppManifestAsync(string code)
        {
            // Using Flurl as Octokit doesn't support this API yet
            // https://github.com/octokit/octokit.net/issues/2138
            var url = $"https://api.github.com/app-manifests/{code}/conversions";

            var response = await url
                .WithHeader("User-Agent", ProductHeader.ToString())
                .PostStringAsync(string.Empty)
                .ConfigureAwait(false);

            var json = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            _app = SimpleJsonSerializer.Deserialize<GitHubAppManifest>(json);

            await SetSecretAsync(_app)
                .ConfigureAwait(false);

            return _app;
        }

        public async Task<GitHubAppManifest> GetAppManifestAsync()
        {
            if (_app is null)
                _app = await GetSecretAsync<GitHubAppManifest>()
                    .ConfigureAwait(false);

            return _app;
        }

        private async Task<T> GetSecretAsync<T>()
        {
            var secret = await secretsService
                .GetSecretAsync(typeof(T).Name)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(secret))
                return default;

            var item = SimpleJsonSerializer.Deserialize<T>(secret);

            return item;
        }

        private async Task SetSecretAsync<T>(T secret)
        {
            var json = SimpleJsonSerializer.Serialize(secret);

            await secretsService
                .SetSecretAsync(typeof(T).Name, json)
                .ConfigureAwait(false);
        }

        public Task HandlePing(string payload, ILogger log = null)
        {
            var ping = SimpleJsonSerializer.Deserialize<WebhookPing>(payload);

            log?.LogWarning($"Received GitHub Webhook: [ EventType: ping, Zen: {ping.Zen ?? "null"} ]");

            return Task.FromResult<IProviderCommand>(null);
        }

        public async Task HandleInstallationAsync(string payload, ILogger log = null)
        {
            var hook = SimpleJsonSerializer.Deserialize<GitHubAppInstallation>(payload);

            await SetSecretAsync(hook.Installation)
                .ConfigureAwait(false);

            log?.LogWarning($"Received GitHub Webhook: [ EventType: installation, Action: {hook.Action ?? "null"} ]");
        }
    }
}
