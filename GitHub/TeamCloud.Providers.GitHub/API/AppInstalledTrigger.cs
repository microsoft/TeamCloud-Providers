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
    public class AppInstalledTrigger
    {
        readonly GitHubService github;

        public AppInstalledTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(AppInstalledTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "installed")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            // json payload from the received webhook
            var query = httpRequest.RequestUri.ParseQueryString();
            var code = query["code"];
            var installation_id = query["installation_id"];
            var setup_action = query["setup_action"];

            Secrets.UserCode = code;
            log.LogWarning($"GitHub code: {code ?? "null"}");


            return new OkResult();
        }
    }
}
