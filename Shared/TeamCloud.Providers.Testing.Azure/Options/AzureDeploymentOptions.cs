/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using TeamCloud.Azure.Deployment;

namespace TeamCloud.Providers.Azure.AppInsights.Options
{
    public sealed class AzureDeploymentOptions : IAzureDeploymentOptions
    {
        public string DefaultLocation => "EastUS";
    }
}
