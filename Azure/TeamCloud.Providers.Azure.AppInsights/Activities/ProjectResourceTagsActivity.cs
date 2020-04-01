/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public class ProjectResourceTagsActivity
    {
        private readonly IAzureResourceService azureResourceService;

        public ProjectResourceTagsActivity(IAzureResourceService azureResourceService)
        {
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        [FunctionName(nameof(ProjectResourceTagsActivity))]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            try
            {
                var (project, resourceId) = functionContext.GetInput<(Project, string)>();

                var resource = await azureResourceService
                    .GetResourceAsync(resourceId, throwIfNotExists: true)
                    .ConfigureAwait(false);

                await resource
                    .SetTagsAsync(project.Tags)
                    .ConfigureAwait(false);
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableException))
            {
                throw serializableException;
            }
        }
    }
}
