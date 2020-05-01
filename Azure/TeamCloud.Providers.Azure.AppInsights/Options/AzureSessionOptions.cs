/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using TeamCloud.Azure;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.AppInsights.Options
{
    [Options("Azure:ResourceManager")]
    public sealed class AzureSessionOptions : IAzureSessionOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string TenantId { get; set; }
    }
}
