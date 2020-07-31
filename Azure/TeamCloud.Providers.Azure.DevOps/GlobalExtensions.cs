/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Operations;
using Microsoft.VisualStudio.Services.WebApi;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Azure.DevOps
{
    internal static class GlobalExtensions
    {
        internal static Dictionary<string, string[]> ToDictionary(this NameValueCollection collection)
            => collection.Cast<string>().ToDictionary(key => key, key => collection.GetValues(key));

        internal static bool TryGetValue(this NameValueCollection collection, string key, out string value)
        {
            value = collection.AllKeys.Contains(key)
                ? collection.Get(key) : default;

            return value != default;
        }

        internal static bool IsProgressStatus(this OperationStatus instance)
            => instance == OperationStatus.Queued
            || instance == OperationStatus.InProgress;

        internal static bool IsErrorStatus(this OperationStatus instance)
            => instance == OperationStatus.Cancelled
            || instance == OperationStatus.Failed
            || instance == OperationStatus.NotSet;

        internal static bool IsFinalStatus(this OperationStatus instance)
            => instance == OperationStatus.Cancelled
            || instance == OperationStatus.Failed
            || instance == OperationStatus.NotSet
            || instance == OperationStatus.Succeeded;

        internal static async IAsyncEnumerable<GraphUser> AsContinousEnumerationAsync(this PagedGraphUsers page, Func<string, Task<PagedGraphUsers>> nextPageCallback)
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

        internal static async IAsyncEnumerable<GraphGroup> AsContinousEnumerationAsync(this PagedGraphGroups page, Func<string, Task<PagedGraphGroups>> nextPageCallback)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page));

            if (nextPageCallback is null)
                throw new ArgumentNullException(nameof(nextPageCallback));

            foreach (var group in page.GraphGroups ?? Enumerable.Empty<GraphGroup>())
                yield return group;

            var continuationToken = page.ContinuationToken?.SingleOrDefault();

            if (!string.IsNullOrEmpty(continuationToken))
            {
                var nextPage = await nextPageCallback(continuationToken)
                    .ConfigureAwait(false);

                await foreach (var group in nextPage.AsContinousEnumerationAsync(nextPageCallback))
                    yield return group;
            }
        }

        internal static async IAsyncEnumerable<T> AsContinousEnumerationAsync<T>(this IPagedList<T> page, Func<string, Task<IPagedList<T>>> nextPageCallback)
        {
            if (page is null)
                throw new ArgumentNullException(nameof(page));

            if (nextPageCallback is null)
                throw new ArgumentNullException(nameof(nextPageCallback));

            foreach (var item in page)
                yield return item;

            var continuationToken = page.ContinuationToken;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                var nextPage = await nextPageCallback(continuationToken)
                    .ConfigureAwait(false);

                await foreach (var item in nextPage.AsContinousEnumerationAsync(nextPageCallback))
                    yield return item;
            }
        }

        internal static async Task<string> GetUserDescriptorAsync(this GraphHttpClient client, Guid originId, IDistributedCache cache = null)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (cache != null)
            {
                var descriptor = await cache
                    .GetStringAsync(GetCacheKey(originId.ToString()))
                    .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(descriptor))
                    return descriptor;
            }

            var page = await client
                .ListUsersAsync()
                .ConfigureAwait(false);

            await foreach (var user in page.AsContinousEnumerationAsync((continuationToken) => client.ListUsersAsync(continuationToken: continuationToken)))
            {
                cache?.SetStringAsync(GetCacheKey(user.OriginId), user.Descriptor, new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromHours(1)
                });

                if (originId.ToString().Equals(user.OriginId, StringComparison.OrdinalIgnoreCase))
                {
                    return user.Descriptor;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(originId));

            string GetCacheKey(string identifier)
                => $"{nameof(GetUserDescriptorAsync)}|{originId}";
        }

        internal static async Task<Guid> GetProjectIdAsync(this ProjectHttpClient client, Project project, IDistributedCache cache = null)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (project is null)
                throw new ArgumentNullException(nameof(project));

            if (cache != null)
            {
                var projectId = await cache
                    .GetStringAsync(GetCacheKey(project.Id))
                    .ConfigureAwait(false);

                if (Guid.TryParse(projectId, out var id))
                    return id;
            }

            var page = await client
                .GetProjects(ProjectState.WellFormed)
                .ConfigureAwait(false);

            await foreach (var azdoProject in page.AsContinousEnumerationAsync((continuationToken) => client.GetProjects(ProjectState.WellFormed, continuationToken: continuationToken)))
            {
                var azdoProjectProperties = await client
                    .GetProjectPropertiesAsync(azdoProject.Id, new string[] { GlobalConstants.TeamCloudProjectIdPropertyKey })
                    .ConfigureAwait(false);

                var projectId = azdoProjectProperties
                    .SingleOrDefault()?
                    .Value as string;

                if (!string.IsNullOrEmpty(projectId))
                {
                    cache?.SetStringAsync(GetCacheKey(projectId), azdoProject.Id.ToString(), new DistributedCacheEntryOptions()
                    {
                        SlidingExpiration = TimeSpan.FromHours(1)
                    });

                    if (project.Id.Equals(projectId))
                    {
                        return azdoProject.Id;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(project));

            string GetCacheKey(string projectId)
                => $"{nameof(GetProjectIdAsync)}|{projectId}";
        }
    }
}
