/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using TeamCloud.Orchestration;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamCloud.Providers.GitHub.Actions.Services;

namespace TeamCloud.Providers.GitHub.Actions
{
    public class StartTrigger
    {
        readonly GitHubService github;

        public StartTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(StartTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "start")] HttpRequestMessage httpRequest, ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var isConfigured = await github
                .IsConfiguredAsync()
                .ConfigureAwait(false);

            if (isConfigured)
                return new ContentResult
                {
                    Content = CompleteHtml,
                    ContentType = "text/html"
                };

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

        private static string Html(string host) =>
$@"
<!DOCTYPE html>
<html lang=""en-US"">
<head>
    <meta charset=""utf-8"">
    <title>GitHub Actions Provider Setup</title>
    <style>
        body {{ font-family: Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.5; color: #24292e; margin: 0; }}
        .intro {{ display: flex; padding-bottom: 8px; padding-top: 40px; margin-bottom: 16px; border-bottom: 1px solid #e1e4e8; flex-flow: row wrap; }}
        form, label, p, h1, button, input, .intro, .note {{ width: 474px; }}
        h1 {{ font-size: 24px; font-weight: 400; flex: 1 1 auto; margin: 0; padding: 0; }}
        .note, label, h1 {{ display: inline-block; }}
        label {{ font-weight: 600; margin: 0 0 6px; }}
        .note {{ min-height: 17px; margin: 4px 0 18px; font-size: 12px; color: #586069; }}
        .intro, form {{ margin: 0 auto; }}
        ul {{ list-style: none; padding: 1em 0 0; margin: 0; }}
        a {{ color: #0366d6; text-decoration: none; }}
        input {{ box-sizing: border-box; border: 1px solid #999; background-color: #fafbfc; min-height: 34px; padding: 6px 8px; line-height: 20px; color: #24292e; vertical-align: middle; background-repeat: no-repeat; background-position: right 8px center; border: 1px solid #d1d5da; border-radius: 3px; outline: none; box-shadow: inset 0 1px 2px rgba(27, 31, 35, .075); }}
        button {{ color: #fff; background-color: #28a745; background-image: linear-gradient(-180deg, #34d058, #28a745 90%); padding: 6px 12px; font-size: 14px; font-weight: 600; line-height: 20px; white-space: nowrap; vertical-align: middle; cursor: pointer; user-select: none; background-repeat: repeat-x; background-position: -1px -1px; background-size: 110% 110%; border: 1px solid rgba(27, 31, 35, .2); border-radius: .25em; -webkit-appearance: none; }}
        input:focus {{ border-color: #2188ff; outline: none; box-shadow: inset 0 1px 2px rgba(27, 31, 35, .075), 0 0 0 0.2em rgba(3, 102, 214, .3); }}
        header {{ background-color: #24292e; height: 53px; }}
    </style>
</head>
<body>
    <header></header>
    <div class=""intro"">
        <h1>GitHub Actions Provider Setup</h1>
    </div>
    <form id=""manifest_form"" action="""" method=""post"">
        <ul>
            <li>
                <label for=""org"">GitHub Organization name</label>
            </li>
            <li>
                <input type=""text"" name=""org"" id=""org"" required minlength=""1"" maxlength=""39"">
            </li>
            <li>
                <p class=""note"">Create a <a href=""https://developer.github.com/apps/about-apps/"">GitHub App</a> to complete the setup. To install the app, you must be an organization owner or have <a href=""https://help.github.com/en/github/setting-up-and-managing-organizations-and-teams/permission-levels-for-an-organization#github-app-managers"">GitHub App manager</a> permissions.</p>
            </li>
            <li class=""button"">
                <button type=""submit"">Create GitHub App</button>
            </li>
            <input type=""hidden"" name=""manifest"" id=""manifest"">
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
                ""default_events"": {{
                    ""workflow_run""
                }},
                ""default_permissions"": {{
                    ""actions"": ""write"",
                    ""administration"": ""write"",
                    ""contents"": ""write"",
                    ""metadata"": ""read"",
                    ""workflows"": ""write"",
                    ""organization_plan"": ""read""
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

        private static string CompleteHtml =>
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
