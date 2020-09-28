/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

namespace TeamCloud.Providers.GitHub.Actions.Data
{
    public class WorkflowRunPayload : GitHubHookPayload
    {
        public WorkflowRun WorkflowRun { get; set; }

        public Workflow Workflow { get; set; }
    }
}
