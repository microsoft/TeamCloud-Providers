/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using TeamCloud.Providers.GitHub.Options;
using TeamCloud.Orchestration;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TeamCloud.Providers.GitHub
{
    public class StartTrigger
    {
        readonly GitHubService github;

        readonly GitHubOptions options;

        public StartTrigger(GitHubService github, GitHubOptions options)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(StartTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "start")] HttpRequestMessage httpRequest, ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var host = await FunctionsEnvironment
                .GetHostUrlAsync()
                .ConfigureAwait(false);

            log.LogWarning(host);

            return new ContentResult
            {
                Content = Html(host),
                ContentType = "text/html"
            };
        }

        private string Html(string host) =>
$@"
<!DOCTYPE html>
<html lang=""en-US"">
<head>
    <meta charset=""utf-8"">
    <title>GitHub Provider Setup</title>
    <style>
        form, label, p, h1, button, input, .intro, .note {{ width: 400px; text-align: center; }}
        h1 {{ font-size: 1.5em; }}
        .note, label, h1 {{ display: inline-block; }}
        .note {{ padding-top: 0.5em; }}
        .intro, form {{ margin: 0 auto; padding: 1em; }}
        form {{ border: 1px solid #CCC; border-radius: 1em; }}
        ul {{ list-style: none; padding: 1em 0 0; margin: 0; }}
        form li+li {{ margin-top: 1em; }}
        input {{ font: 1em sans-serif; box-sizing: border-box; border: 1px solid #999; }}
        input, button {{ padding: 0.5em; }}
        input:focus {{ border-color: #000; }}
    </style>
</head>
<body>
    <div class=""intro"">
        <h1>GitHub Provider Setup</h1>
    </div>
    <form id=""manifest_form"" action="""" method=""post"">
        <p>To complete the GitHub Provider setup, you will create a <a href=""https://developer.github.com/apps/about-apps/"">GitHub App</a> and install it in your GitHub Organization.</p>
        <p>Enter the name of the GitHub Organization the provider will use to create resources associated with TeamCloud projects.</p>
        <ul>
            <li>
                <input type=""text"" name=""org"" id=""org"" placeholder=""GitHub Organization Name"" required minlength=""1"" maxlength=""39"">
            </li>
            <li class=""button"">
                <button type=""submit"">Create App</button>
            </li>
            <input type=""hidden"" name=""manifest"" id=""manifest"">
            <li>
                <p class=""note""><b><em>Note: To install the app, you must be an organization owner or have <a href=""https://help.github.com/en/github/setting-up-and-managing-organizations-and-teams/permission-levels-for-an-organization#github-app-managers"">GitHub App manager</a> permissions.</em></b></p>
            </li>
        </ul>
    </form>
    <script>
        const btn = document.querySelector(""button"")
        function updateManifest() {{
            let org = document.getElementById(""org"").value;
            document.getElementById(""manifest_form"").action = ""https://github.com/organizations/"" + org + ""/settings/apps/new"";
            manifest = document.getElementById(""manifest"");
            manifest.value = JSON.stringify({{
                ""name"": ""TeamCloud-"" + org,
                ""url"": ""https://github.com/microsoft/TeamCloud"",
                ""public"": false,
                ""redirect_url"": ""{host}/api/created"",
                ""hook_attributes"": {{
                    ""url"": ""{host}/api/events"",
                    ""active"": true
                }},
                ""default_permissions"": {{
                    ""actions"": ""write"",
                    ""administration"": ""write"",
                    ""checks"": ""write"",
                    ""contents"": ""write"",
                    ""issues"": ""write"",
                    ""metadata"": ""read"",
                    ""pull_requests"": ""write"",
                    ""repository_projects"": ""admin"",
                    ""vulnerability_alerts"": ""read"",
                    ""workflows"": ""write"",
                    ""members"": ""write"",
                    ""organization_administration"": ""write"",
                    ""organization_plan"": ""read"",
                    ""organization_projects"": ""admin"",
                    ""team_discussions"": ""write"",
                    ""emails"": ""read""
                }}
            }});
        }}
        btn.addEventListener(""click"", function () {{
            updateManifest();
        }})
    </script>
</body>
</html>
";
    }
}
