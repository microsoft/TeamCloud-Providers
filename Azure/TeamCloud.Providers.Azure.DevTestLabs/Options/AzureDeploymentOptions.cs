using System;
using TeamCloud.Azure.Deployment;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.DevTestLabs.Options
{
    [Options]
    public class AzureDeploymentOptions : IAzureDeploymentOptions
    {
        private readonly AzureResourceManagerOptions azureResourceManagerOptions;

        public AzureDeploymentOptions(AzureResourceManagerOptions azureResourceManagerOptions)
        {
            this.azureResourceManagerOptions = azureResourceManagerOptions ?? throw new ArgumentNullException(nameof(azureResourceManagerOptions));
        }

        public string Region => azureResourceManagerOptions.Region;
    }
}
