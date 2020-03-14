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
using TeamCloud.Azure.Deployment;
using TeamCloud.Model.Commands;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevTestLabs.Templates;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public class ProjectCreateActivity
    {
        private readonly IAzureDeploymentService azureDeploymentService;

        public ProjectCreateActivity(IAzureDeploymentService azureDeploymentService)
        {
            this.azureDeploymentService = azureDeploymentService ?? throw new ArgumentNullException(nameof(azureDeploymentService));
        }

        [FunctionName(nameof(ProjectCreateActivity))]
        [RetryOptions(3)]
        public async Task<Dictionary<string, string>> RunActivity(
            [ActivityTrigger] ProviderProjectCreateCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var vnetPrefix = "10.0.0.0/16";
            var snetPrefix = new object[]
            {
                new
                {
                    subnetname = "TeamCloud",
                    subnetprefix = "10.0.0.0/24"
                },
                new
                {
                    subnetname = "AzureBastionSubnet",
                    subnetprefix = "10.0.1.0/24"
                }
            };

            var template = new ProjectCreateTemplate();

            template.Parameters["ProjectName"] = command.Payload.Name;
            //template.Parameters["Repositories"] = Array.Empty<object>();
            //template.Parameters["ImageGallery"] = "";
            template.Parameters["LabPublicEnvironments"] = "Enabled";
            template.Parameters["LabPublicArtifacts"] = "Enabled";
            template.Parameters["LabVNetPrefix"] = vnetPrefix;
            template.Parameters["LabSNetPrefix"] = snetPrefix;

            var deployment = await azureDeploymentService
                .DeployResourceGroupTemplateAsync(template, command.Payload.ResourceGroup.SubscriptionId, command.Payload.ResourceGroup.ResourceGroupName)
                .ConfigureAwait(false);

            var deploymentOutput = await deployment
                .WaitAndGetOutputAsync(throwOnError: true)
                .ConfigureAwait(false);

            return deploymentOutput
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());

        }
    }
}
