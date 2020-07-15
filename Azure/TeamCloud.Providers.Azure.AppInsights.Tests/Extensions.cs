/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Newtonsoft.Json.Linq;

namespace TeamCloud.Providers.Azure.AppInsights
{
    internal static class Extensions
    {
        public static object SetValue(this JValue instance, object value)
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            return instance.Value = value;
        }
    }
}
