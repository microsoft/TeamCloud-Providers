/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
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

        readonly GitHubOptions options;

        public TestTrigger(GitHubService github, GitHubOptions options)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(TestTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "test/{projectName}")] HttpRequestMessage httpRequest,
            string projectName,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            log.LogWarning(Secrets.Log());

            var project = new Project
            {
                Name = projectName
            };

            var team = await github.CreateTeam(project).ConfigureAwait(false);

            log.LogWarning(team.ToString());

            var repo = await github.CreateRepository(project).ConfigureAwait(false);

            log.LogWarning(repo.ToString());

            var githubProject = await github.CreateProject(project, repo.Id).ConfigureAwait(false);

            log.LogWarning(githubProject.ToString());

            return new OkResult();
        }
    }
}
