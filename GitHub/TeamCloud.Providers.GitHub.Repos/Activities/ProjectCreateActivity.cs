/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace TeamCloud.Providers.GitHub.Repos.Activities
{
    public class ProjectCreateActivity
    {
        private readonly GitHubService github;
        private readonly IAzureSessionService azureSessionService;

        public ProjectCreateActivity(GitHubService github, IAzureSessionService azureSessionService)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ProjectCreateActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
        public async Task RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            using (log.BeginProjectScope(project))
            {
                try
                {
                    var (team, repo, proj) = await github
                        .CreateTeamCloudProjectAsync(project)
                        .ConfigureAwait(false);

                    var tasks = GetProjectLinks(team, repo, proj, project.Id)
                        .Select(l => project.Links.Links.SetAsync(azureSessionService, l));

                    await Task.WhenAll(tasks)
                        .ConfigureAwait(false);

                    log.LogInformation($"Created GitHub resources for project.");
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectCreateActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }

        private static List<ProjectLink> GetProjectLinks(Octokit.Team team, Octokit.Repository repo, Octokit.Project proj, string projectId)
            => new List<ProjectLink>
                {
                    new ProjectLink
                    {
                        Title = "GitHub Team",
                        HRef = $"{team.Organization.Url.Replace("api.", string.Empty, StringComparison.OrdinalIgnoreCase)}/teams/{team.Slug}",
                        Type = ProjectLinkType.Link
                    }.WithGeneratedId("team", projectId),
                    new ProjectLink
                    {
                        Title = "GitHub Repo",
                        HRef = repo.HtmlUrl,
                        Type = ProjectLinkType.GitRepository
                    }.WithGeneratedId("repo", projectId),
                    new ProjectLink
                    {
                        Title = "GitHub Project",
                        HRef = $"{proj.OwnerUrl.Replace("api.", string.Empty, StringComparison.OrdinalIgnoreCase)}/projects/{proj.Number}",
                        Type = ProjectLinkType.Link
                    }.WithGeneratedId("project", projectId)
                };
    }

    internal static class ProjectCreateExtension
    {
        public static Task CreateProjectAsync(this IDurableOrchestrationContext orchestrationContext, Project project)
            => orchestrationContext.CallActivityWithRetryAsync(nameof(ProjectCreateActivity), project);
    }
}
