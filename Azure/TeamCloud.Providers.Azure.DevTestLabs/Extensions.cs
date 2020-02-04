/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Linq;
using Microsoft.AspNetCore.Http;

namespace TeamCloud.Providers.Azure.DevTestLabs
{
    internal static class Extensions
    {
        internal static string GetCallbackUrl(this IHeaderDictionary headerDictionary)
        {
            return headerDictionary.TryGetValue("x-functions-callback", out var value) ? value.FirstOrDefault() : null;
        }

    }
}
