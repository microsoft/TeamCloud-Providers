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
using TeamCloud.Providers.GitHub.Options;

namespace TeamCloud.Providers.GitHub
{
    public class TestTrigger
    {
        readonly GitHubService github;

        readonly GitHubOptions options;

        public TestTrigger(GitHubService github, GitHubOptions options)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(TestTrigger))]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "test")] HttpRequestMessage httpRequest)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            return new ContentResult
            {
                Content = Html,
                ContentType = "text/html"
            };
        }

        private string Html =>
$@"
<html>
<head>
  <title>Test</title>
</head>
<body>

<form action=""https://github.com/organizations/{options.OrganizationName}/settings/apps/new"" method=""post"">
  Create a GitHub App Manifest: <input type=""text"" name=""manifest"" id=""manifest""><br>
  <input type=""submit"" value=""Submit"">
</form>

<script>
  input = document.getElementById(""manifest"")
  input.value = JSON.stringify({{
    ""name"": ""TeamCloud-Test"",
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
      ""repository_projects"": ""write"",
      ""vulnerability_alerts"": ""read"",
      ""workflows"": ""write"",
      ""members"": ""write"",
      ""organization_administration"": ""write"",
      ""organization_plan"": ""read"",
      ""organization_projects"": ""write"",
      ""team_discussions"": ""write"",
      ""emails"": ""read""
    }},
    ""callback_url"": ""{Secrets.ProviderUrl}/api/installed""
    ""setup_url"": ""{Secrets.ProviderUrl}/api/setup"",
  }})
</script>
</body>
";

    }
}
