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
using TeamCloud.Providers.GitHub.Data;
using TeamCloud.Providers.GitHub.Services;

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "created")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var query = httpRequest.RequestUri.ParseQueryString();
            var code = query["code"];

            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("Parameter 'code' is required in the query string.");

            log.LogWarning($"GitHub code: {code ?? "null"}");

            var app = await github
                .GetAppManifest(code)
                .ConfigureAwait(false);

            if (app is null)
                return new NotFoundObjectResult("Failed to get GitHub App Manifiest from GitHub.");

            return new ContentResult
            {
                Content = Html(app),
                ContentType = "text/html"
            };
        }

        private string Html(GitHubAppManifest app) =>
$@"
<!DOCTYPE html>
<html lang=""en-US"">
<head>
    <meta charset=""utf-8"">
    <title>GitHub Provider Setup</title>
    <style>
        form, p, h1, button, .intro {{ width: 400px; text-align: center; }}
        h1 {{ font-size: 1.5em; }}
        .note, h1 {{ display: inline-block; }}
        .note {{ padding-top: 0.5em; }}
        .intro, form {{ margin: 0 auto; padding: 1em; }}
        form {{ border: 1px solid #CCC; border-radius: 1em; }}
        ul {{ list-style: none; padding: 1em 0 0; margin: 0; }}
        form li+li {{ margin-top: 1em; }}
        button {{ padding: 0.5em; }}
    </style>
</head>
<body>
    <div class=""intro"">
        <h1>GitHub Provider Setup</h1>
    </div>
    <form id=""manifest_form"" action=""https://github.com/apps/{app.Slug}/installations/new/permissions"">
        <p>The GitHub App was successfully created! Click the button below to install the App into your GitHub Organization.</p>
        <ul>
            <input type=""hidden"" name=""target_id"" value=""{app.Owner.Id}"">
            <li class=""button"">
                <button type=""submit"">Install App</button>
            </li>
            <li>
                <p class=""note""><b><em>Note: To install the app, you must be an organization owner or have <a href=""https://help.github.com/en/github/setting-up-and-managing-organizations-and-teams/permission-levels-for-an-organization#github-app-managers"">GitHub App manager</a> permissions.</em></b></p>
            </li>
        </ul>
    </form>
</body>
</html>
";
    }
}
