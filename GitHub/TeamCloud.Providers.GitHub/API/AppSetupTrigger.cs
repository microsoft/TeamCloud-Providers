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
        // public async Task<IActionResult> Run(
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "setup")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var query = httpRequest.RequestUri.ParseQueryString();
            var installation_id = query["installation_id"];
            var setup_action = query["setup_action"];
            log.LogWarning($"GitHub installation_id: {installation_id ?? "null"}");
            log.LogWarning($"GitHub setup_action: {setup_action ?? "null"}");

            log.LogWarning(Secrets.Log());

            // var html = $"<html><head><title>Test</title></head><body><p>GitHub app successfully installed. Click <a href=\"https://github.com/login/oauth/authorize?client_id={Secrets.App.ClientId}&redirect_uri={Secrets.ProviderUrl}/api/installed&login={Secrets.Installer.Login}\">here</a> to install the app into your user and complete the setup.</p></body>";
            var html = $"<html><head><title>Test</title></head><body><p>GitHub app successfully installed. Return to the CLI.</p></body>";

            return new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };
        }
    }
}
