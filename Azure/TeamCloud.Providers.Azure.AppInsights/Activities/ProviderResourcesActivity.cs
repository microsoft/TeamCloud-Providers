using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Azure.Resources;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public class ProviderResourcesActivity
    {
        private readonly IAzureResourceService azureResourceService;

        public ProviderResourcesActivity(IAzureResourceService azureResourceService)
        {
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        public async Task<IEnumerable<string>> RunActivity(
            [ActivityTrigger] string resourceGroupId)
        {
            if (resourceGroupId is null)
                throw new ArgumentNullException(nameof(resourceGroupId));

            if (!AzureResourceIdentifier.TryParse(resourceGroupId, out var resourceGroupIdValid))
                throw new ArgumentOutOfRangeException(nameof(resourceGroupId));

            var resourceGroup = await azureResourceService
                .GetResourceGroupAsync(resourceGroupIdValid.SubscriptionId, resourceGroupIdValid.ResourceGroup, throwIfNotExists: true)
                .ConfigureAwait(false);

            return Enumerable.Empty<string>();
        }
    }
}
