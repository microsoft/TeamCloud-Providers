/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Serialization;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Providers.GitHub.Repos.Services;
using TeamCloud.Model.Commands;
using Newtonsoft.Json;
using TeamCloud.Providers.GitHub.Repos.Data;

namespace TeamCloud.Providers.GitHub.Repos.Activities
{
    public class ComponentCreateActivity
    {
        private readonly GitHubService github;
        private readonly IAzureSessionService azureSessionService;

        public ComponentCreateActivity(GitHubService github, IAzureSessionService azureSessionService)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ComponentCreateActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
        public async Task<Component> RunActivity(
            [ActivityTrigger] ProviderComponentCreateCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var component = command.Payload;

            using (log.BeginCommandScope(command))
            {
                try
                {
                    if (component.OfferId.Equals("github.repos.repo", StringComparison.Ordinal))
                    {
                        log.LogInformation($"Getting Project.");

                        var project = await command.Links.Project
                            .GetAsync<Project>(azureSessionService)
                            .ConfigureAwait(false);

                        log.LogInformation($"Creating Repository.");

                        var input = JsonConvert.DeserializeObject<RepoComponentInput>(component.InputJson);

                        var repo = await github
                            .CreateRepositoryAsync(project, input)
                            .ConfigureAwait(false);

                        log.LogInformation($"Saving Component.");

                        var repoComponent = new RepoComponent
                        {
                            Id = repo.Id,
                            Name = repo.Name,
                            Private = repo.Private,
                            FullName = repo.FullName,
                            IsTemplate = repo.IsTemplate,
                            Description = repo.Description,
                            DefaultBranch = repo.DefaultBranch,
                            Url = repo.Url,
                            HtmlUrl = repo.HtmlUrl,
                            CloneUrl = repo.CloneUrl,
                            GitUrl = repo.GitUrl,
                            SshUrl = repo.SshUrl,
                            SvnUrl = repo.SvnUrl,
                            MirrorUrl = repo.MirrorUrl,
                        };

                        component.DisplayName = repoComponent.Name;
                        component.Description = repoComponent.Description;
                        component.ValueJson = JsonConvert.SerializeObject(repoComponent, Formatting.Indented);

                        component = await command.Links.ProjectComponents
                            .PostAsync(azureSessionService, component)
                            .ConfigureAwait(false);

                        log.LogInformation($"Creating Link.");

                        var link = new ProjectLink
                        {
                            Title = repoComponent.Name,
                            HRef = repoComponent.HtmlUrl,
                            Type = ProjectLinkType.GitRepository
                        }.WithGeneratedId($"{component.OfferId}.{component.Id}", project.Id);

                        link = await project.Links.Links
                            .PostAsync(azureSessionService, link)
                            .ConfigureAwait(false);
                    }

                    return component;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ComponentCreateActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }

    internal static class ComponentCreateExtentions
    {
        public static Task<Component> CreateComponentAsync(this IDurableOrchestrationContext orchestrationContext, ProviderComponentCreateCommand command)
            => orchestrationContext.CallActivityWithRetryAsync<Component>(nameof(ComponentCreateActivity), command);
    }
}
