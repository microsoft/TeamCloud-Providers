/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TeamCloud.Providers.GitHub.Options;

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
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "start")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            log.LogWarning(NewAppUrl);

            return new ContentResult
            {
                Content = Html,
                ContentType = "text/html"
            };
        }

        private string NewAppUrl =>
            new StringBuilder()
            .Append($"https://github.com/organizations/{options.OrganizationName}/settings/apps/new")
            .Append($"?name=TeamCloud-{options.OrganizationName}")
            .Append($"&description=TeamCloud%20GitHub%20Provider.")
            .Append($"&url=https://github.com/microsoft/TeamCloud")
            // .Append($"&callback_url={CALLBACK_URL}")
            // .Append($"&request_oauth_on_install=true")
            .Append($"&setup_url={Secrets.ProviderUrl}/api/setup")
            .Append($"&public=false")
            .Append($"&webhook_url={Secrets.ProviderUrl}/api/events")
            .Append($"&webhook_active=true")
            .Append($"&actions=write")
            .Append($"&administration=write")
            .Append($"&checks=write")
            .Append($"&contents=write")
            .Append($"&issues=write")
            .Append($"&metadata=read")
            .Append($"&pull_requests=write")
            .Append($"&repository_projects=admin")
            .Append($"&vulnerability_alerts=read")
            .Append($"&workflows=write")
            .Append($"&members=write")
            .Append($"&organization_administration=write")
            .Append($"&organization_plan=read")
            .Append($"&organization_projects=admin")
            .Append($"&team_discussions=write")
            .Append($"&emails=read")
            .ToString();

        private string Html =>
$@"
<html>
<head>
  <title>Test</title>
</head>
<body>

<form action=""https://github.com/organizations/{options.OrganizationName}/settings/apps/new"" method=""post"">
  Create a GitHub App Manifest: <input type=""text"" name=""manifest"" id=""manifest"">
  <input type=""submit"" value=""Submit"">
</form>

<script>
  input = document.getElementById(""manifest"")
  input.value = JSON.stringify({{
    ""name"": ""TeamCloud-{options.OrganizationName}"",
    ""url"": ""https://github.com/microsoft/TeamCloud"",
    ""public"": false,
    ""redirect_url"": ""{Secrets.ProviderUrl}/api/created"",
    ""hook_attributes"": {{
      ""url"": ""{Secrets.ProviderUrl}/api/events"",
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
  }})
</script>
</body>
";

    }
}
