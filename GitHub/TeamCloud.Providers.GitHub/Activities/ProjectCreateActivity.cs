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
// using TeamCloud.Providers.GitHub.Templates;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.GitHub.Activities
{
    public class ProjectCreateActivity
    {
        private readonly GitHubService github;

        public ProjectCreateActivity(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
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
                    await github.CreateTeamCloudProject(project, log)
                        .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectCreateActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
