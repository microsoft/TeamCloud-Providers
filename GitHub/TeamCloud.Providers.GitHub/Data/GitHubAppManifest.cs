/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    public class GitHubAppManifest : GitHubApp
    {
        public GitHubAppManifest() { }

        public GitHubAppManifest(long id, string name, User owner, string description, Uri externalUrl, Uri htmlUrl, DateTimeOffset createdAt, DateTimeOffset updatedAt)
            : base(id, name, owner, description, externalUrl?.AbsoluteUri, htmlUrl?.AbsoluteUri, createdAt, updatedAt) { }

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
