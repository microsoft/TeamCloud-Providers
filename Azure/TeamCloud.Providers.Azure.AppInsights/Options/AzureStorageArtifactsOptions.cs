/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Azure.Deployment.Providers;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.AppInsights.Options
{
    [Options("Azure:DeploymentStorage")]
    public sealed class AzureStorageArtifactsOptions : IAzureStorageArtifactsOptions
    {
        public string BaseUrlOverride { get; set; }

        public string ConnectionString { get; set; } = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    }
}
