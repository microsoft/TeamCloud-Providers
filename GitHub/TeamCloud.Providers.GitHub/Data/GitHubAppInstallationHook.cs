/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    public class GitHubAppInstallationHook
    {
        public string Action { get; set; }

        public InstallationWithSlug Installation { get; set; }

        public User Sender { get; set; }
    }
}
