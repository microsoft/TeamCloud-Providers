/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TeamCloud.Providers.GitHub.Actions.Data
{
    [SuppressMessage("Design", "CA2227: Collection properties should be read only", Justification = "GitHub API payload models")]
    public class WorkflowJobs
    {
        public int TotalCount { get; set; }
        public List<WorkflowJob> Jobs { get; set; } = new List<WorkflowJob>();
    }
}