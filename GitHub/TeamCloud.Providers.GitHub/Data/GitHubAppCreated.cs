/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    public class GitHubAppCreated : GitHubApp
    {
        public string Slug { get; set; }

        public string NodeId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string WebhookSecret { get; set; }
        public string Pem { get; set; }
    }
}
