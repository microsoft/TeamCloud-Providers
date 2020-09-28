/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace TeamCloud.Providers.GitHub.Data
{
    [SuppressMessage("Design", "CA1056: URI properties should not be strings", Justification = "GitHub API payload models")]
    public class WorkflowStep
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Conclusion { get; set; }
        public int Number { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
    }
}
