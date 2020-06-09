/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

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

        private string Html =>
$@"
<!DOCTYPE html>
<html lang=""en-US"">
<head>
    <meta charset=""utf-8"">
    <title>GitHub Provider Setup</title>
    <style>
        .form_box, p, h1, .intro {{ width: 400px; text-align: center; }}
        h1 {{ font-size: 1.5em; display: inline-block; text-align: center; }}
        .intro, .form_box {{ margin: 0 auto; padding: 1em; }}
        .form_box {{ border: 1px solid #CCC; border-radius: 1em; }}
        .form_box li+li {{ margin-top: 1em; }}
    </style>
</head>
<body>
    <div class=""intro"">
        <h1>GitHub Provider Setup</h1>
    </div>
    <div class=""form_box"">
        <p>GitHub Provider setup is complete!</p>
        <p>You can safely close this window and return to the CLI.</p>
    </div>
</body>
</html>
";
    }
}
