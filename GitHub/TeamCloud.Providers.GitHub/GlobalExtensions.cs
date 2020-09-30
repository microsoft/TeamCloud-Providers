/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using TeamCloud.Providers.GitHub.Data;

namespace TeamCloud.Providers.GitHub
{
    public static class GlobalExtensions
    {
        public static string GitHubEventType(this HttpRequestMessage httpRequest)
            => (httpRequest ?? throw new ArgumentNullException(nameof(httpRequest)))
            .Headers.TryGetValues("X-GitHub-Event", out var values) ? values.FirstOrDefault() : null;

        public static string GitHubDeliveryId(this HttpRequestMessage httpRequest)
            => (httpRequest ?? throw new ArgumentNullException(nameof(httpRequest)))
            .Headers.TryGetValues("X-GitHub-Delivery", out var values) ? values.FirstOrDefault() : null;

        public static string GitHubSignature(this HttpRequestMessage httpRequest)
            => (httpRequest ?? throw new ArgumentNullException(nameof(httpRequest)))
            .Headers.TryGetValues("X-Hub-Signature", out var values) ? values.FirstOrDefault() : null;

        public static bool ContainsKeyInsensitive(this IDictionary<string, string> dict, string key)
            => dict.GetValueInsensitive(key) != default;

        public static string GetValueInsensitive(this IDictionary<string, string> dict, string key)
        {
            var ikey = dict?.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

            return ikey is null ? null : dict[ikey];
        }

        public static bool Completed(this WorkflowRunPayload payload)
            => payload?.Action?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Requested(this WorkflowRunPayload payload)
            => payload?.Action?.Equals("requested", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool IsSentBy(this WorkflowRunPayload payload, GitHubAppManifest app)
            => payload?.Sender?.Login?.Contains((app ?? throw new ArgumentNullException(nameof(app))).Slug, StringComparison.OrdinalIgnoreCase) ?? false;



        public static bool Queued(this WorkflowRun run)
            => run?.Status?.Equals("queued", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool InProgress(this WorkflowRun run)
            => run?.Status?.Equals("in_progress", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Completed(this WorkflowRun run)
            => run?.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false;


        public static bool ConclusionSucceeded(this WorkflowRun run)
            => run?.Conclusion?.Equals("success", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionFailed(this WorkflowRun run)
            => run?.Conclusion?.Equals("failure", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionNeutral(this WorkflowRun run)
            => run?.Conclusion?.Equals("neutral", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionCancelled(this WorkflowRun run)
            => run?.Conclusion?.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionSkipped(this WorkflowRun run)
            => run?.Conclusion?.Equals("skipped", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionTimedOut(this WorkflowRun run)
            => run?.Conclusion?.Equals("timed_out", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionActionRequiered(this WorkflowRun run)
            => run?.Conclusion?.Equals("action_required", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool ConclusionActionStale(this WorkflowRun run)
            => run?.Conclusion?.Equals("stale", StringComparison.OrdinalIgnoreCase) ?? false;


        public static bool Failed(this WorkflowJob job)
            => job?.Conclusion?.Equals("failure", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Succeeded(this WorkflowJob job)
            => job?.Conclusion?.Equals("success", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Skipped(this WorkflowJob job)
            => job?.Conclusion?.Equals("skipped", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
