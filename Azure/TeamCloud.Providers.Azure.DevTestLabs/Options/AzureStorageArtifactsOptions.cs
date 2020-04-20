/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Azure.Deployment.Providers;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.DevTestLabs.Options
{
    [Options("Azure:DeploymentStorage")]
    public class AzureStorageArtifactsOptions : IAzureStorageArtifactsOptions
    {
        public string BaseUrlOverride { get; set; }

        private string connectionString;

        public string ConnectionString
        {
            get => string.IsNullOrEmpty(connectionString) ? Environment.GetEnvironmentVariable("AzureWebJobsStorage") : connectionString;
            set => connectionString = value;
        }
    }
}
