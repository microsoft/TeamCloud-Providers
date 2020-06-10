/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Operations;

namespace TeamCloud.Providers.Azure.DevOps
{
    internal static class GlobalExtensions
    {
        public static Dictionary<string, string[]> ToDictionary(this NameValueCollection collection)
            => collection.Cast<string>().ToDictionary(key => key, key => collection.GetValues(key));


        public static bool TryGetValue(this NameValueCollection collection, string key, out string value)
        {
            value = collection.AllKeys.Contains(key)
                ? collection.Get(key) : default;

            return value != default;
        }

        public static bool IsProgressStatus(this OperationStatus instance)
            => instance == OperationStatus.Queued
            || instance == OperationStatus.InProgress;

        public static bool IsErrorStatus(this OperationStatus instance)
            => instance == OperationStatus.Cancelled
            || instance == OperationStatus.Failed
            || instance == OperationStatus.NotSet;

        public static bool IsFinalStatus(this OperationStatus instance)
            => instance == OperationStatus.Cancelled
            || instance == OperationStatus.Failed
            || instance == OperationStatus.NotSet
            || instance == OperationStatus.Succeeded;

        public static async IAsyncEnumerable<GraphUser> AsContinousEnumerationAsync(this PagedGraphUsers page, Func<string, Task<PagedGraphUsers>> nextPageCallback)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page));

            if (nextPageCallback is null)
                throw new ArgumentNullException(nameof(nextPageCallback));

            foreach (var user in page.GraphUsers ?? Enumerable.Empty<GraphUser>())
                yield return user;

            var continuationToken = page.ContinuationToken?.SingleOrDefault();

            if (!string.IsNullOrEmpty(continuationToken))
            {
                var nextPage = await nextPageCallback(continuationToken)
                    .ConfigureAwait(false);

                await foreach (var user in nextPage.AsContinousEnumerationAsync(nextPageCallback))
                    yield return user;
            }
        }

        public static async Task<string> GetUserDescriptorAsync(this GraphHttpClient client, Guid originId, IMemoryCache cache = null)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (cache != null && cache.TryGetValue<string>($"{nameof(GetUserDescriptorAsync)}|{originId}", out var descriptor))
            {
                return descriptor;
            }

            var page = await client
                .ListUsersAsync()
                .ConfigureAwait(false);

            await foreach (var user in page.AsContinousEnumerationAsync((continuationToken) => client.ListUsersAsync(continuationToken: continuationToken)))
            {
                cache?.Set($"{nameof(GetUserDescriptorAsync)}|{user.OriginId}", user.Descriptor, new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });

                if (originId.ToString().Equals(user.OriginId, StringComparison.OrdinalIgnoreCase))
                {
                    return user.Descriptor;
                }
            }

            return null;
        }
    }
}
