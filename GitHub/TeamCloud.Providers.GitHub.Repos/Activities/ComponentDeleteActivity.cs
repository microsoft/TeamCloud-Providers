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
    public class ComponentDeleteActivity
    {
        private readonly GitHubService github;
        private readonly IAzureSessionService azureSessionService;

        public ComponentDeleteActivity(GitHubService github, IAzureSessionService azureSessionService)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ComponentDeleteActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
        public async Task<Component> RunActivity(
            [ActivityTrigger] IDurableActivityContext activitiyContext,
            ILogger log)
        {
            if (activitiyContext is null)
                throw new ArgumentNullException(nameof(activitiyContext));

            var functionInput = activitiyContext.GetInput<Input>();

            var command = functionInput.Command;
            var component = functionInput.Component;

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

                        log.LogInformation($"Deleting Repository.");

                        var repoComponent = JsonConvert.DeserializeObject<RepoComponent>(component.ValueJson);

                        await github
                            .DeleteRepositoryAsync(repoComponent)
                            .ConfigureAwait(false);

                        log.LogInformation($"Deleting Component.");

                        await command.Links.ProjectComponents
                            .DeleteAsync(azureSessionService, component)
                            .ConfigureAwait(false);

                        log.LogInformation($"Deleting Link.");

                        var link = new ProjectLink
                        {
                            Title = repoComponent.Name,
                            HRef = repoComponent.HtmlUrl,
                            Type = ProjectLinkType.GitRepository
                        }.WithGeneratedId($"{component.OfferId}.{component.Id}", project.Id);

                        await project.Links.Links
                           .DeleteAsync(azureSessionService, link)
                           .ConfigureAwait(false);
                    }

                    return component;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ComponentDeleteActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }

        internal struct Input
        {
            public Component Component { get; set; }
            public IProviderCommand Command { get; set; }
        }
    }

    internal static class ComponentDeleteExtentions
    {
        public static Task<Component> DeleteComponentAsync(this IDurableOrchestrationContext orchestrationContext, IProviderCommand command, Component component)
            => orchestrationContext.CallActivityWithRetryAsync<Component>(nameof(ComponentDeleteActivity), new ComponentDeleteActivity.Input { Command = command, Component = component });
    }
}
