/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Serialization;
using TeamCloud.Providers.GitHub.Repos.Services;

namespace TeamCloud.Providers.GitHub.Repos.Activities
{
    public class ProjectDeleteActivity
    {
        private readonly GitHubService github;

        public ProjectDeleteActivity(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(ProjectDeleteActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
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
                    await github
                        .DeleteTeamCloudProjectAsync(project)
                        .ConfigureAwait(false);

                    log.LogInformation($"Deleted GitHub resources for project.");
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectDeleteActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
