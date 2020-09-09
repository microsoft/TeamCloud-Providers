/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using TeamCloud.Providers.GitHub.Services;

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
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "setup")] HttpRequestMessage httpRequest)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            // var query = httpRequest.RequestUri.ParseQueryString();
            // var installation_id = query["installation_id"];
            // var setup_action = query["setup_action"];

            // log.LogWarning($"GitHub installation_id: {installation_id ?? "null"}");
            // log.LogWarning($"GitHub setup_action: {setup_action ?? "null"}");

            return new ContentResult
            {
                Content = Html,
                ContentType = "text/html"
            };
        }

        private static string Html =>
$@"
<!DOCTYPE html>
<html lang=""en-US"">
<head>
    <meta charset=""utf-8"">
    <title>GitHub Provider Setup</title>
    <style>
        body {{ font-family: Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.5; color: #24292e; margin: 0; }}
        .intro {{ display: flex; padding-bottom: 8px; padding-top: 40px; margin-bottom: 16px; border-bottom: 1px solid #e1e4e8; flex-flow: row wrap; }}
        .form_box, h1, .intro {{ width: 474px; }}
        h1 {{ display: inline-block; font-size: 24px; font-weight: 400; flex: 1 1 auto; margin: 0; padding: 0; }}
        .intro, .form_box {{ margin: 0 auto; }}
        header {{ background-color: #24292e; height: 53px; }}
    </style>
</head>
<body>
    <header></header>
    <div class=""intro"">
        <h1>GitHub Provider Setup</h1>
    </div>
    <div class=""form_box"">
        <p>Setup is complete. You can safely close this window and return to the CLI.</p>
    </div>
</body>
</html>
";
    }
}
