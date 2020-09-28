/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    public class GitHubAppInstallation
    {
        public string Action { get; set; }

        public InstallationWithSlug Installation { get; set; }

        public User Sender { get; set; }
    }
}
