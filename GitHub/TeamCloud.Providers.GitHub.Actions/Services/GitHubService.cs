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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit.Internal;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Providers.GitHub.Data;
using TeamCloud.Providers.GitHub.Services;

namespace TeamCloud.Providers.GitHub.Actions.Services
{
    public class GitHubService
    {
        private readonly SimpleJsonSerializer SimpleJsonSerializer = new SimpleJsonSerializer();

        private readonly GitHubAppService githubAppService;

        public GitHubService(GitHubAppService githubAppService)
        {
            this.githubAppService = githubAppService ?? throw new ArgumentNullException(nameof(githubAppService));
        }

        public async Task SendRepositoryEventAsync(string repo, IProviderCommand command, ILogger log = null)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            var payload = JsonConvert.SerializeObject(new RepositoryDispatchEvent
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
            ProviderProjectCreateCommand _ => "tc_project_create",
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

        private async Task<WorkflowJobs> GetWorkflowJobsAsync(WorkflowRun workflowRun, ILogger log = null)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var jobsResponse = await client
                .Connection
                .Get<WorkflowJobs>(new Uri(workflowRun.JobsUrl), new Dictionary<string, string>(), "application/vnd.github.v3+json")
                .ConfigureAwait(false);

            if (jobsResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception("Failed to get jobs for workflow run");

            return jobsResponse.Body;
        }

        private async Task<IProviderCommand> GetProviderCommandAsync(WorkflowRun workflowRun, ILogger log = null)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

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

        internal async Task<(IProviderCommand, ICommandResult)> HandleWorkflowRunAsync(string payload, ILogger log = null)
        {
            var run = SimpleJsonSerializer.Deserialize<WorkflowRunPayload>(payload);

            log?.LogWarning($"Received GitHub Webhook: [ EventType: workflow_run, Action: {run.Action ?? "null"}, Status: {run.WorkflowRun?.Status ?? "null"}, Conclusion: {run.WorkflowRun?.Conclusion ?? "null"} ]");

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            if (!run.IsSentBy(app))
            {
                log?.LogWarning($"[ Ignoring workflow run because sender '{run.Sender?.Login ?? "null"}' does not containt app slug '{app.Slug}'");
                return (null, null);
            }

            if (run.Completed() && run.WorkflowRun.Completed())
            {
                var command = await GetProviderCommandAsync(run.WorkflowRun, log)
                    .ConfigureAwait(false);

                var commandResult = command.CreateResult();

                if (run.WorkflowRun.ConclusionFailed())
                {
                    log?.LogWarning("WorkflowRun Conclusion was 'failure'");
                    commandResult.Errors.Add(new Exception($"GitHub workflow '{run.Workflow.Name}' completed with conclusion 'failure'. You may view results at: {run.WorkflowRun.HtmlUrl}"));
                }

                return (command, commandResult);
            }

            return (null, null);
        }

        internal Task HandleWebhookAsync(string eventType, string payload, ILogger log = null)
            => eventType?.ToUpperInvariant() switch
            {
                "PING" => githubAppService.HandlePing(payload, log),
                "INSTALLATION" => githubAppService.HandleInstallationAsync(payload, log),
                _ => HandleOtherAsync(eventType, payload, log)
            };

        private Task HandleOtherAsync(string eventType, string payload, ILogger log = null)
        {
            var hook = SimpleJsonSerializer.Deserialize<WebhookPayload>(payload);

            if (!eventType.Equals("workflow_run", StringComparison.OrdinalIgnoreCase))
            {
                log?.LogWarning($"Received GitHub Webhook: [ EventType: {eventType}, Action: {hook.Action ?? "null"} ]");
                // log?.LogWarning(payload);
            }

            return Task.FromResult(hook);
        }
    }
}
