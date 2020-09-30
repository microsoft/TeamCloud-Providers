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
using TeamCloud.Providers.GitHub.Data;
using TeamCloud.Providers.GitHub.Services;

namespace TeamCloud.Providers.GitHub
{
    public class AppCreatedTrigger
    {
        readonly GitHubAppService github;

        public AppCreatedTrigger(GitHubAppService github)
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

            var code = httpRequest.RequestUri.ParseQueryString().GetValues("code")?.FirstOrDefault();

            if (string.IsNullOrEmpty(code))
                throw new InvalidOperationException("Parameter 'code' is required in the query string.");

            log.LogWarning($"GitHub code: {code ?? "null"}");

            var app = await github
                .ConvertAppManifestAsync(code)
                .ConfigureAwait(false);

            if (app is null)
                return new NotFoundObjectResult("Failed to get GitHub App Manifiest from GitHub.");

            return new ContentResult
            {
                Content = Html(app),
                ContentType = "text/html"
            };
        }

        private static string Html(GitHubAppManifest app) =>
$@"
<!DOCTYPE html>
<html lang=""en-US"">
<head>
    <meta charset=""utf-8"">
    <title>GitHub Provider Setup</title>
    <style>
        body {{ font-family: Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.5; color: #24292e; margin: 0; }}
        .intro {{ display: flex; padding-bottom: 8px; padding-top: 40px; margin-bottom: 16px; border-bottom: 1px solid #e1e4e8; flex-flow: row wrap; }}
        form, h1, button, .intro, .note {{ width: 474px; }}
        h1 {{ font-size: 24px; font-weight: 400; flex: 1 1 auto; margin: 0; padding: 0; }}
        .note, h1 {{ display: inline-block; }}
        .note {{ min-height: 17px; margin: 4px 0 18px; font-size: 12px; color: #586069; }}
        .intro, form {{ margin: 0 auto; }}
        ul {{ list-style: none; padding: 2em 0 0; margin: 0; }}
        a {{ color: #0366d6; text-decoration: none; }}
        button {{ color: #fff; background-color: #28a745; background-image: linear-gradient(-180deg, #34d058, #28a745 90%); padding: 6px 12px; font-size: 14px; font-weight: 600; line-height: 20px; white-space: nowrap; vertical-align: middle; cursor: pointer; user-select: none; background-repeat: repeat-x; background-position: -1px -1px; background-size: 110% 110%; border: 1px solid rgba(27, 31, 35, .2); border-radius: .25em; -webkit-appearance: none; }}
        header {{ background-color: #24292e; height: 53px; }}
    </style>
</head>
<body>
    <header></header>
    <div class=""intro"">
        <h1>GitHub Provider Setup</h1>
    </div>
    <form id=""manifest_form"" action=""https://github.com/apps/{app.Slug}/installations/new/permissions"">
        <ul>
            <input type=""hidden"" name=""target_id"" value=""{app.Owner.Id}"">
            <li class=""button"">
                <button type=""submit"">Install GitHub App on {app.Owner.Login}</button>
            </li>
            <li>
                <p class=""note"">To install the app, you must be an organization owner or have <a href=""https://help.github.com/en/github/setting-up-and-managing-organizations-and-teams/permission-levels-for-an-organization#github-app-managers"">GitHub App manager</a> permissions.</p>
            </li>
        </ul>
    </form>
</body>
</html>
";
    }
}
