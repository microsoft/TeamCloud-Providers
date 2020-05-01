/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Azure.Deployment;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.DevTestLabs.Options
{
    [Options("Azure:Deployment")]
    public sealed class AzureDeploymentOptions : IAzureDeploymentOptions
    {
        public string DefaultLocation { get; set; } = Environment.GetEnvironmentVariable("REGION_NAME");
    }
}
