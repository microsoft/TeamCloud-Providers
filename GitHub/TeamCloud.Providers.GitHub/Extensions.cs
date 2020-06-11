/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
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

        public static bool ContainsKeyInsensitive(this IDictionary<string, string> dict, string key)
            => dict.GetValueInsensitive(key) != default;

        public static string GetValueInsensitive(this IDictionary<string, string> dict, string key)
        {
            var ikey = dict.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

            return ikey is null ? null : dict[ikey];
        }
    }
}
