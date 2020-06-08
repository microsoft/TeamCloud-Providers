/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Data;
using TeamCloud.Providers.GitHub.Options;

namespace TeamCloud.Providers.GitHub
{
    public class TestTrigger
    {
        readonly GitHubService github;

        public TestTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(TestTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "delete", Route = "test/{projectName}")] HttpRequestMessage httpRequest,
            string projectName,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            log.LogWarning(Secrets.Log());

            var guid = Guid.NewGuid().ToString();
            var project = new Project
            {
                Id = guid,
                Name = projectName,
                Users = new List<User> {
                    new User {
                        Id = "1ab004bb-57c6-4217-b3cf-f63d090e5b28",
                        Role = TeamCloudUserRole.Admin,
                        ProjectMemberships = new List<ProjectMembership> {
                            new ProjectMembership {
                                ProjectId = guid,
                                Role = ProjectUserRole.Owner,
                                Properties = new Dictionary<string, string> {
                                    { "GitHubLogin", "colbylwilliams" }
                                }
                            }
                        },
                        Properties = new Dictionary<string, string> {
                            { "GitHubLogin", "colbylwilliams" }
                        }
                    }
                }
            };

            if (httpRequest.Method == HttpMethod.Get)
            {
                var (team, repo, proj) = await github.CreateTeamCloudProject(project, log)
                    .ConfigureAwait(false);

                log.LogWarning(team.ToString());

                log.LogWarning(repo.ToString());

                log.LogWarning(proj.ToString());
            }
            else if (httpRequest.Method == HttpMethod.Delete)
            {
                await github.DeleteTeamCloudProject(project, log)
                    .ConfigureAwait(false);
            }

            return new OkResult();
        }
    }
}
