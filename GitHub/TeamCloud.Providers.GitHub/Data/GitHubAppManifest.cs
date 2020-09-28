/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    [SuppressMessage("Design", "CA1054: URI parameters should not be strings", Justification = "GitHub API payload models")]
    public class GitHubAppManifest : GitHubApp
    {
        public GitHubAppManifest() { }

        public GitHubAppManifest(long id, string name, User owner, string description, string externalUrl, string htmlUrl, DateTimeOffset createdAt, DateTimeOffset updatedAt)
            : base(id, name, owner, description, externalUrl, htmlUrl, createdAt, updatedAt) { }

        public string Slug { get; set; }

        public string NodeId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string WebhookSecret { get; set; }

        public string Pem { get; set; }
    }
}
