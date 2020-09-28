/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using TeamCloud.Providers.GitHub.Actions.Data;

namespace TeamCloud.Providers.GitHub.Actions
{
    internal static class Extensions
    {
        public static string GitHubEventType(this HttpRequestMessage httpRequest)
            => httpRequest.Headers.TryGetValues("X-GitHub-Event", out var values) ? values.FirstOrDefault() : null;

        public static string GitHubDeliveryId(this HttpRequestMessage httpRequest)
            => httpRequest.Headers.TryGetValues("X-GitHub-Delivery", out var values) ? values.FirstOrDefault() : null;

        public static string GitHubSignature(this HttpRequestMessage httpRequest)
            => httpRequest.Headers.TryGetValues("X-Hub-Signature", out var values) ? values.FirstOrDefault() : null;

        public static bool ContainsKeyInsensitive(this IDictionary<string, string> dict, string key)
            => dict.GetValueInsensitive(key) != default;

        public static string GetValueInsensitive(this IDictionary<string, string> dict, string key)
        {
            var ikey = dict.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

            return ikey is null ? null : dict[ikey];
        }

        public static bool Completed(this WorkflowRunPayload payload)
            => payload.Action?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Requested(this WorkflowRunPayload payload)
            => payload.Action?.Equals("requested", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool IsSentBy(this WorkflowRunPayload payload, GitHubAppManifest app)
            => payload.Sender?.Login?.Contains(app.Slug, StringComparison.OrdinalIgnoreCase) ?? false;



        public static bool Completed(this WorkflowRun run)
            => run.Status?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Queued(this WorkflowRun run)
            => run.Status?.Equals("queued", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Failed(this WorkflowRun run)
            => run.Conclusion?.Equals("failure", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Succeeded(this WorkflowRun run)
            => run.Conclusion?.Equals("success", StringComparison.OrdinalIgnoreCase) ?? false;


        public static bool Failed(this WorkflowJob job)
            => job.Conclusion?.Equals("failure", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Succeeded(this WorkflowJob job)
            => job.Conclusion?.Equals("success", StringComparison.OrdinalIgnoreCase) ?? false;

        public static bool Skipped(this WorkflowJob job)
            => job.Conclusion?.Equals("skipped", StringComparison.OrdinalIgnoreCase) ?? false;

    }
}

