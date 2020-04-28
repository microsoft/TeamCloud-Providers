/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;

namespace TeamCloud.Providers.GitHub
{
    public class AppSetupTrigger
    {
        readonly GitHubService github;

        public AppSetupTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(AppSetupTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "setup")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var html = $"<html><head><title>Test</title></head><body><p>GitHub app successfully installed. Click <a href=\"https://github.com/login/oauth/authorize?client_id={Secrets.App.ClientId}&redirect_uri={Secrets.ProviderUrl}/api/installed&login={Secrets.Installer.Login}\">here</a> to install the app into your user and complete the setup.</p></body>";

            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }
    }
}
