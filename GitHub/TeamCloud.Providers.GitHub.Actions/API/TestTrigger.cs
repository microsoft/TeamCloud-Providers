/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeamCloud.Providers.GitHub.Actions.Services;
using TeamCloud.Model.Commands;
using Newtonsoft.Json;
using System.Linq;

namespace TeamCloud.Providers.GitHub.Actions
{
    public class TestTrigger
    {
        readonly GitHubService github;

        public TestTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(TestTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "test")] HttpRequestMessage httpRequest)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var body = await httpRequest.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            var repo = httpRequest.RequestUri.ParseQueryString().GetValues("repo")?.FirstOrDefault();

            if (string.IsNullOrEmpty(repo))
                return new BadRequestObjectResult($"The following query string paramaters are required: 'repo'");

            var command = JsonConvert.DeserializeObject<ProviderProjectCreateCommand>(body);

            // var command = JsonConvert.DeserializeObject<ProviderProjectCreateCommand>(CommandJson());

            await github.SendRepositoryEventAsync(repo, command)
                .ConfigureAwait(false);

            return new OkResult();
        }

        private static string CommandJson() =>
$@"
{{
  ""$type"": ""TeamCloud.Model.Commands.ProviderProjectCreateCommand, TeamCloud.Model"",
  ""providerId"": ""github.actions"",
  ""commandId"": ""99985fd9-57c5-4f55-a8eb-158740832a8b"",
  ""projectId"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
  ""user"": {{
    ""id"": ""13aa3876-30b0-40b1-8518-cf5777b5c133"",
    ""userType"": ""User"",
    ""role"": ""Admin"",
    ""projectMemberships"": [
      {{
        ""projectId"": ""b96e6fae-3dfd-414b-a081-80641a6a7caa"",
        ""role"": ""Provider"",
        ""properties"": {{
          ""foo"": ""bar""
        }}
      }},
      {{
        ""projectId"": ""0a70f567-71e1-4e4e-b88c-5a6e3da0893b"",
        ""role"": ""Provider""
      }},
      {{
        ""projectId"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
        ""role"": ""Owner""
      }}
    ],
    ""properties"": {{
      ""GitHubLogin"": ""colbylwilliams""
    }}
  }},
  ""payload"": {{
    ""id"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
    ""name"": ""ProjectSix"",
    ""type"": {{
      ""id"": ""default"",
      ""isDefault"": true,
      ""region"": ""eastus"",
      ""subscriptions"": [
        ""456bb1ba-04ae-481a-9c55-0e5c1bef0f65""
      ],
      ""subscriptionCapacity"": 10,
      ""providers"": [
        {{
          ""id"": ""github.actions""
        }}
      ]
    }},
    ""resourceGroup"": {{
      ""id"": ""/subscriptions/456bb1ba-04ae-481a-9c55-0e5c1bef0f65/resourceGroups/ProjectSix_m73phrdv2yecc"",
      ""name"": ""ProjectSix_m73phrdv2yecc"",
      ""subscriptionId"": ""456bb1ba-04ae-481a-9c55-0e5c1bef0f65"",
      ""region"": ""eastus""
    }},
    ""users"": [
      {{
        ""id"": ""13aa3876-30b0-40b1-8518-cf5777b5c133"",
        ""userType"": ""User"",
        ""role"": ""Admin"",
        ""projectMemberships"": [
          {{
            ""projectId"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
            ""role"": ""Owner""
          }}
        ],
        ""properties"": {{
          ""GitHubLogin"": ""colbylwilliams""
        }}
      }}
    ],
    ""_links"": {{
      ""_self"": {{
        ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d""
      }},
      ""identity"": {{
        ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d/identity""
      }},
      ""users"": {{
        ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d/users""
      }},
      ""links"": {{
        ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d/links""
      }}
    }}
  }},
  ""_links"": {{
    ""systemData"": {{
      ""href"": ""https://localhost:5001/api/providers/github.actions""
    }},
    ""projectData"": {{
      ""href"": ""https://localhost:5001/api/providers/github.actions""
    }}
  }}
}}
";
    }
}
