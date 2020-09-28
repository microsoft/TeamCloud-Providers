/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace TeamCloud.Providers.GitHub.Actions.Data
{
    [SuppressMessage("Design", "CA1056: URI properties should not be strings", Justification = "GitHub API payload models")]
    public class Workflow
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string State { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string BadgeUrl { get; set; }
    }
}
