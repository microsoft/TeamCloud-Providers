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
    public class AppCreatedTrigger
    {
        readonly GitHubService github;

        public AppCreatedTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(AppCreatedTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "created")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var query = httpRequest.RequestUri.ParseQueryString();
            var code = query["code"];

            log.LogWarning($"GitHub code: {code ?? "null"}");

            var app = await github
                .GetManifest(code)
                .ConfigureAwait(false);

            var html = $"<html><head><title>Test</title></head><body><p>GitHub app successfully created. Click <a href=\"https://github.com/apps/{app.Slug}/installations/new/permissions?target_id={app.Owner.Id}\">here</a> to install the app into your org and complete the setup.</p></body>";

            var result = new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };

            return result;
        }
    }
}
