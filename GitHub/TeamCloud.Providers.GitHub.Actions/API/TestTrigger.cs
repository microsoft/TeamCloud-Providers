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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequestMessage httpRequest, ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            var command = JsonConvert.DeserializeObject<ProviderProjectCreateCommand>(CommandJson());

            await github.SendRepositoryEventAsync("workflow-provider", command, log)
                .ConfigureAwait(false);

            return new OkResult();
        }

        private static string CommandJson() =>
$@"
{{
    ""$type"": ""TeamCloud.Model.Commands.ProviderProjectCreateCommand, TeamCloud.Model"",
    ""_links"": {{
        ""projectData"": {{
            ""href"": ""https://localhost:5001/api/providers/github.actions""
        }},
        ""systemData"": {{
            ""href"": ""https://localhost:5001/api/providers/github.actions""
        }}
    }},
    ""commandId"": ""99985fd9-57c5-4f55-a8eb-158740632a8b"",
    ""payload"": {{
        ""_links"": {{
            ""_self"": {{
                ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d""
            }},
            ""identity"": {{
                ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d/identity""
            }},
            ""links"": {{
                ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d/links""
            }},
            ""users"": {{
                ""href"": ""https://localhost:5001/api/projects/1ba9a7e9-b0a0-4354-a4db-d58b9e58906d/users""
            }}
        }},
        ""id"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
        ""name"": ""ProjectSix"",
        ""resourceGroup"": {{
            ""id"": ""/subscriptions/456bb1ba-04ae-481a-9c55-0e5c1bef0f65/resourceGroups/ProjectSix_m73phrdv2yecc"",
            ""name"": ""ProjectSix_m73phrdv2yecc"",
            ""region"": ""eastus"",
            ""subscriptionId"": ""456bb1ba-04ae-481a-9c55-0e5c1bef0f65""
        }},
        ""type"": {{
            ""id"": ""default"",
            ""isDefault"": true,
            ""providers"": [
                {{
                    ""id"": ""github.actions""
                }}
            ],
            ""region"": ""eastus"",
            ""subscriptionCapacity"": 10,
            ""subscriptions"": [
                ""456bb1ba-04ae-481a-9c55-0e5c1bef0f65""
            ]
        }},
        ""users"": [
            {{
                ""id"": ""13aa3876-30b0-40b1-8518-cf5777b5c133"",
                ""projectMemberships"": [
                    {{
                        ""projectId"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
                        ""role"": ""Owner""
                    }}
                ],
                ""properties"": {{
                    ""GitHubLogin"": ""colbylwilliams""
                }},
                ""role"": ""Admin"",
                ""userType"": ""User""
            }}
        ]
    }},
    ""projectId"": ""1ba9a7e9-b0a0-4354-a4db-d58b9e58906d"",
    ""providerId"": ""github.actions"",
    ""user"": {{
        ""id"": ""13aa3876-30b0-40b1-8518-cf5777b5c133"",
        ""projectMemberships"": [
            {{
                ""projectId"": ""b96e6fae-3dfd-414b-a081-80641a6a7caa"",
                ""properties"": {{
                    ""foo"": ""bar""
                }},
                ""role"": ""Provider""
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
        }},
        ""role"": ""Admin"",
        ""userType"": ""User""
    }}
}}
";
    }
}
