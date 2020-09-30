/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Octokit;

namespace TeamCloud.Providers.GitHub.Data
{
    [SuppressMessage("Design", "CA1056: URI properties should not be strings", Justification = "GitHub API payload models")]
    [SuppressMessage("Design", "CA2227: Collection properties should be read only", Justification = "GitHub API payload models")]
    public class WorkflowRun
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string HeadBranch { get; set; }
        public string HeadSha { get; set; }
        public int RunNumber { get; set; }
        public string Event { get; set; }
        public string Status { get; set; } // can be: queued, in_progress, completed
        public string Conclusion { get; set; } // can be: success, failure, neutral, cancelled, skipped, timed_out, action_required, stale
        public long WorkflowId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public IList<PullRequest> PullRequests { get; set; } = new List<PullRequest>();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string JobsUrl { get; set; }
        public string LogsUrl { get; set; }
        public string CheckSuiteUrl { get; set; }
        public string ArtifactsUrl { get; set; }
        public string CancelUrl { get; set; }
        public string RerunUrl { get; set; }
        public string WorkflowUrl { get; set; }
        public Commit HeadCommit { get; set; }
        public Commit Repository { get; set; }
        public Commit HeadRepository { get; set; }
    }
}
