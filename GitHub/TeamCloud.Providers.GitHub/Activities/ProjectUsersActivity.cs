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
using TeamCloud.Serialization;
using TeamCloud.Providers.GitHub.Services;

namespace TeamCloud.Providers.GitHub.Activities
{
    public class ProjectUsersActivity
    {
        private readonly GitHubService github;

        public ProjectUsersActivity(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(ProjectUsersActivity))]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (project, teamId) = functionContext.GetInput<(Project, int)>();

            using (log.BeginProjectScope(project))
            {
                try
                {
                    // var resource = await azureResourceService
                    //     .GetResourceAsync(resourceId, throwIfNotExists: true)
                    //     .ConfigureAwait(false);

                    // var permissions = project.Users
                    //     .ToRoleAssignments(role => role.Equals(UserRoles.Project.Owner, StringComparison.OrdinalIgnoreCase)
                    //         ? new Octokit.RepositoryPermissions(true, true, true)
                    //         : new Octokit.RepositoryPermissions(false, true, true);

                    // await resource
                    //     .SetRoleAssignmentsAsync(roleAssignments)
                    //     .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectUsersActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
