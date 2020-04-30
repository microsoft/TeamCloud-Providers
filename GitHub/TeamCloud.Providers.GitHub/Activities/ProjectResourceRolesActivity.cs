/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.GitHub.Activities
{
    public class ProjectResourceRolesActivity
    {
        private readonly GitHubService github;

        public ProjectResourceRolesActivity(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(ProjectResourceRolesActivity))]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (project, resourceId) = functionContext.GetInput<(Project, string)>();

            using (log.BeginProjectScope(project))
            {
                try
                {
                    var roleAssignments = (project.Users ?? Enumerable.Empty<User>())
                        .ToDictionary(usr => usr.Id, usr => Enumerable.Repeat(GetTeamRole(usr), 1));

                    if (roleAssignments.Any())
                    {
                        // await github
                        //     .SetRoleAssignmentsAsync(roleAssignments)
                        //     .ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectResourceRolesActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }

            static Octokit.TeamRole GetTeamRole(User user) => user.Role switch
            {
                UserRoles.Project.Owner => Octokit.TeamRole.Maintainer,
                UserRoles.Project.Member => Octokit.TeamRole.Member,
                _ => throw new NotSupportedException($"User '{user.Id}' has an unsupported role '{user.Role}'")
            };
        }
    }
}
