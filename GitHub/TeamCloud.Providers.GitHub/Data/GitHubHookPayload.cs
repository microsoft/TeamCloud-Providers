/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    public class GitHubHookPayload : ActivityPayload
    {
        public string Action { get; set; }

        public Organization Organization { get; set; }
    }
}
