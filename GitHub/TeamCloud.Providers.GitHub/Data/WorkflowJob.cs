/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TeamCloud.Providers.GitHub.Data
{
    [SuppressMessage("Design", "CA1056: URI properties should not be strings", Justification = "GitHub API payload models")]
    [SuppressMessage("Design", "CA2227: Collection properties should be read only", Justification = "GitHub API payload models")]
    public class WorkflowJob
    {
        public long Id { get; set; }
        public long RunId { get; set; }
        public string RunUrl { get; set; }
        public string NodeId { get; set; }
        public string HeadSha { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string Status { get; set; }
        public string Conclusion { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public string Name { get; set; }
        public IList<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
        public string CheckRunUrl { get; set; }
    }
}
