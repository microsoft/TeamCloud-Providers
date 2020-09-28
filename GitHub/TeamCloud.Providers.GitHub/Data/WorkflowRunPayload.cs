/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

namespace TeamCloud.Providers.GitHub.Data
{
    public class WorkflowRunPayload : WebhookPayload
    {
        public WorkflowRun WorkflowRun { get; set; }

        public Workflow Workflow { get; set; }
    }
}
