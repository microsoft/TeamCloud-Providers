/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TeamCloud.Providers.GitHub.Data
{
    [SuppressMessage("Design", "CA2227: Collection properties should be read only", Justification = "GitHub API payload models")]
    public class WorkflowArtifacts
    {
        public int TotalCount { get; set; }
        public List<WorkflowArtifact> Artifacts { get; set; } = new List<WorkflowArtifact>();
    }
}
