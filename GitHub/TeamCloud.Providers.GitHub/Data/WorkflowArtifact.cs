/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace TeamCloud.Providers.GitHub.Data
{
    [SuppressMessage("Design", "CA1056: URI properties should not be strings", Justification = "GitHub API payload models")]
    public class WorkflowArtifact
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public long SizeInBytes { get; set; }
        public string Url { get; set; }
        public string ArchiveDownloadUrl { get; set; }
        public bool Expired { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
