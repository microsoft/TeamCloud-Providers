/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;
using Octokit.Internal;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Providers.GitHub.Actions.Data;

namespace TeamCloud.Providers.GitHub.Actions.Services
{
    public class GitHubService
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

        public GitHubService(ISecretsService secretsService)
        {
            this.secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var app = await GetAppManifestAsync()
                .ConfigureAwait(false);

            return app != default;
        }

        private async Task<GitHubClient> GetAppClientAsync()
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

        public async Task<GitHubAppManifest> GetAppManifestAsync(string code)
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

        private async Task<GitHubAppManifest> GetAppManifestAsync()
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

        public async Task SendRepositoryEventAsync(string repo, IProviderCommand command, ILogger log)
        {
            var client = await GetAppClientAsync().ConfigureAwait(false);
            var app = await GetAppManifestAsync().ConfigureAwait(false);

            // var url = $"https://api.github.com/repos/{app.Owner.Login}/{repo}/dispatches";

            var payload = JsonConvert.SerializeObject(new RepoDispatchPayload
            {
                EventType = GetEventType(command),
                ClientPayload = command
            });

            // var statusCode = await client
            var result = await client
                .Connection
                .Post<HttpStatusCode>(new Uri($"/repos/{app.Owner.Login}/{repo}/dispatches", uriKind: UriKind.Relative), payload, "application/vnd.github.v3+json", "application/json")
                // .Post(new Uri($"/repos/{app.Owner.Login}/{repo}/dispatches", uriKind: UriKind.Relative), payload, "application/vnd.github.v3+json")
                .ConfigureAwait(false);

            // foreach (var header in result.HttpResponse.Headers)
            //     log.LogWarning($"{header.Key} : {header.Value}");

            // var response = await url
            //     .WithHeader("User-Agent", ProductHeader.ToString())
            //     .WithHeader("Accept", "application/vnd.github.v3+json")
            //     .PostStringAsync(payload)
            //     .ConfigureAwait(false);

            // var json = await response.Content
            //     .ReadAsStringAsync()
            //     .ConfigureAwait(false);
        }

        private static string GetEventType(IProviderCommand command) => command switch
        {
            ProviderEventCommand _ => "tc_event",
            // ProviderProjectCreateCommand _ => "tc_project_create",
            ProviderProjectCreateCommand _ => "tc_project_test",
            ProviderProjectDeleteCommand _ => "tc_project_delete",
            ProviderProjectUpdateCommand _ => "tc_project_update",
            ProviderProjectUserCreateCommand _ => "tc_project_user_create",
            ProviderProjectUserDeleteCommand _ => "tc_project_user_delete",
            ProviderProjectUserUpdateCommand _ => "tc_project_user_update",
            ProviderRegisterCommand _ => "tc_register",
            ProviderTeamCloudUserCreateCommand _ => "tc_teamcloud_user_create",
            ProviderTeamCloudUserDeleteCommand _ => "tc_teamcloud_user_delete",
            ProviderTeamCloudUserUpdateCommand _ => "tc_teamcloud_user_update",
            _ => throw new NotSupportedException()
        };

        public async Task<Repository> CreateRepositoryAsync(Model.Data.Project project)
        {
            var client = await GetAppClientAsync().ConfigureAwait(false);
            var app = await GetAppManifestAsync().ConfigureAwait(false);

            var gitHubProvider = project?.Type?.Providers?.FirstOrDefault(p => p.Id == "github.actions");

            var newRepository = new NewRepository(project.Name)
            {
                AutoInit = true,
                Description = $"Repository for TeamCloud project {project.Name}",
                LicenseTemplate = gitHubProvider?.Properties.GetValueInsensitive(AvailableProperties.LicenseTemplate),
                GitignoreTemplate = gitHubProvider?.Properties.GetValueInsensitive(AvailableProperties.GitignoreTemplate)
            };

            var repository = await client
                .Repository
                .Create(app.Owner.Login, newRepository)
                .ConfigureAwait(false);

            return repository;
        }

        public async Task DeleteRepositoryAsync(Model.Data.Project project)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            var client = await GetAppClientAsync().ConfigureAwait(false);
            var app = await GetAppManifestAsync().ConfigureAwait(false);

            try
            {
                await client
                    .Repository
                    .Delete(app.Owner.Login, project.Name)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return; // already deleted, swallow execption
            }
        }

        private async Task<WorkflowJobs> GetWorkflowJobsAsync(WorkflowRun workflowRun, ILogger log)
        {
            var client = await GetAppClientAsync().ConfigureAwait(false);

            var jobsResponse = await client
                .Connection
                .Get<WorkflowJobs>(new Uri(workflowRun.JobsUrl), new Dictionary<string, string>(), "application/vnd.github.v3+json")
                .ConfigureAwait(false);

            if (jobsResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception("Failed to get jobs for workflow run");

            return jobsResponse.Body;
        }

        private async Task<IProviderCommand> GetProviderCommandAsync(WorkflowRun workflowRun, ILogger log)
        {
            var client = await GetAppClientAsync().ConfigureAwait(false);

            var artifactsResponse = await client
                .Connection
                .Get<WorkflowArtifacts>(new Uri(workflowRun.ArtifactsUrl), new Dictionary<string, string>(), "application/vnd.github.v3+json")
                .ConfigureAwait(false);

            if (artifactsResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception("Failed to get artifacts from workflow run");

            var artifact = artifactsResponse
                .Body
                .Artifacts
                .FirstOrDefault(a => a.Name.Equals("command", StringComparison.OrdinalIgnoreCase));

            if (artifact is null)
                throw new Exception("No artifact named 'command' found on completed workflow run.");

            var downloadResponse = await client
                .Connection
                .GetRaw(new Uri(artifact.ArchiveDownloadUrl), new Dictionary<string, string>())
                .ConfigureAwait(false);

            if (downloadResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception("Failed to download owrkflow artifact archive zip");

            var _tempName = $"{workflowRun.Id}_command";
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"{_tempName}.zip");
            var tempJsonPath = Path.Combine(Path.GetTempPath(), $"{_tempName}.json");

            await File.WriteAllBytesAsync(tempZipPath, downloadResponse.Body)
                .ConfigureAwait(false);

            using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Read))
            {
                var entry = archive.GetEntry("command.json");
                entry.ExtractToFile(tempJsonPath);
            }

            var commandJson = await File.ReadAllTextAsync(tempJsonPath)
                .ConfigureAwait(false);

            var command = JsonConvert.DeserializeObject<IProviderCommand>(commandJson);

            return command;
        }

        internal async Task<ICommandResult> HandleWorkflowRunAsync(string payload, ILogger log)
        {
            var run = SimpleJsonSerializer.Deserialize<WorkflowRunPayload>(payload);

            log.LogWarning($"Received GitHub Webhook: [ EventType: workflow_run, Action: {run.Action ?? "null"}, Status: {run.WorkflowRun?.Status ?? "null"}, Conclusion: {run.WorkflowRun?.Conclusion ?? "null"} ]");

            var app = await GetAppManifestAsync().ConfigureAwait(false);

            if (!run.IsSentBy(app))
            {
                log.LogWarning($"[ Ignoring workflow run because sender '{run.Sender?.Login ?? "null"}' does not containt app slug '{app.Slug}'");
                return null;
            }

            if (run.Completed() && run.WorkflowRun.Completed())
            {
                // log.LogWarning($"WorkflowRun Url: {run.WorkflowRun.Url}");
                // log.LogWarning($"WorkflowRun ArtifactsUrl: {run.WorkflowRun.ArtifactsUrl}");

                var command = await GetProviderCommandAsync(run.WorkflowRun, log)
                    .ConfigureAwait(false);

                if (run.WorkflowRun.Succeeded())
                {
                    // log.LogWarning($"Command.commandId: {command.CommandId}");
                    // log.LogWarning($"Command Payload: {command.Payload}");

                    return command.CreateResult();
                }
                else if (run.WorkflowRun.Failed())
                {
                    log.LogWarning("WorkflowRun Conclusion was not 'success'");
                    // log.LogWarning(payload);

                    var commandResult = command.CreateResult();
                    commandResult.Errors.Add(new Exception($"GitHub workflow '{run.Workflow.Name}' failed. You may view results at: {run.WorkflowRun.HtmlUrl}"));
                    return commandResult;
                }
            }
            else if (run.Action.Equals("requested", StringComparison.OrdinalIgnoreCase))
            {

            }

            return null;
        }

        internal Task HandleWebhookAsync(string eventType, string payload, ILogger log)
            => eventType?.ToUpperInvariant() switch
            {
                "PING" => HandlePing(payload, log),
                "INSTALLATION" => HandleInstallationAsync(payload, log),
                // "WORKFLOW_RUN" => HandleWorkflowRunAsync(payload, log),
                _ => HandleOtherAsync(eventType, payload, log)
            };

        private Task HandlePing(string payload, ILogger log)
        {
            var ping = SimpleJsonSerializer.Deserialize<GitHubPing>(payload);

            log.LogWarning($"Received GitHub Webhook: [ EventType: ping, Zen: {ping.Zen ?? "null"} ]");

            return Task.FromResult<IProviderCommand>(null);
        }

        private async Task HandleInstallationAsync(string payload, ILogger log)
        {
            var hook = SimpleJsonSerializer.Deserialize<GitHubAppInstallationHook>(payload);

            await SetSecretAsync(hook.Installation)
                .ConfigureAwait(false);

            log.LogWarning($"Received GitHub Webhook: [ EventType: installation, Action: {hook.Action ?? "null"} ]");
        }

        private Task HandleOtherAsync(string eventType, string payload, ILogger log)
        {
            var hook = SimpleJsonSerializer.Deserialize<GitHubHookPayload>(payload);

            if (!eventType.Equals("workflow_run", StringComparison.OrdinalIgnoreCase))
                log.LogWarning($"Received GitHub Webhook: [ EventType: {eventType}, Action: {hook.Action ?? "null"} ]");
            // log?.LogWarning(payload);

            return Task.FromResult(hook);
        }
    }
}
