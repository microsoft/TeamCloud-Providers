/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public class ProjectResourceListActivity
    {
        private readonly IAzureResourceService azureResourceService;

        public ProjectResourceListActivity(IAzureResourceService azureResourceService)
        {
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        [FunctionName(nameof(ProjectResourceListActivity))]
        public async Task<IEnumerable<string>> RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            if (project.ResourceGroup is null)
                return Enumerable.Empty<string>();

            using (log.BeginProjectScope(project))
            {
                try
                {
                    var resourceGroup = await azureResourceService
                        .GetResourceGroupAsync(project.ResourceGroup.SubscriptionId, project.ResourceGroup.ResourceGroupName, throwIfNotExists: true)
                        .ConfigureAwait(false);

                    var resources = resourceGroup
                        .GetResourcesByTypeAsync("Microsoft.DevTestLab/labs");

                    return await resources
                        .SelectAwait(resource => new ValueTask<string>(resource.ResourceId.ToString()))
                        .ToArrayAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectResourceListActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
