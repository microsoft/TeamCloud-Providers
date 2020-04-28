/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Net.Http;

namespace TeamCloud.Providers.GitHub
{
    internal static class Extensions
    {
        public static string GitHubEventType(this HttpRequestMessage httpRequest)
            => httpRequest.Headers.TryGetValues("X-GitHub-Event", out var values) ? values.FirstOrDefault() : null;

        public static string GitHubDeliveryId(this HttpRequestMessage httpRequest)
            => httpRequest.Headers.TryGetValues("X-GitHub-Delivery", out var values) ? values.FirstOrDefault() : null;

        public static string GitHubSignature(this HttpRequestMessage httpRequest)
            => httpRequest.Headers.TryGetValues("X-Hub-Signature", out var values) ? values.FirstOrDefault() : null;
    }
}
