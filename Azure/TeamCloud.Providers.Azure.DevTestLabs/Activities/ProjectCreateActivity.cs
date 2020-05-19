/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Deployment;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevTestLabs.Templates;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public class ProjectCreateActivity
    {
        private readonly IAzureDeploymentService azureDeploymentService;

        public ProjectCreateActivity(IAzureDeploymentService azureDeploymentService)
        {
            this.azureDeploymentService = azureDeploymentService ?? throw new ArgumentNullException(nameof(azureDeploymentService));
        }

        [FunctionName(nameof(ProjectCreateActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
        public async Task<string> RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            using (log.BeginProjectScope(project))
            {
                try
                {
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

                    template.Parameters["ProjectName"] = project.Name;
                    //template.Parameters["Repositories"] = Array.Empty<object>();
                    //template.Parameters["ImageGallery"] = "";
                    template.Parameters["LabBastionHostEnabled"] = false;
                    template.Parameters["LabMarketplaceEnabled"] = false;
                    template.Parameters["LabPublicEnvironmentsEnabled"] = false;
                    template.Parameters["LabPublicArtifactsEnabled"] = false;
                    template.Parameters["LabVNetPrefix"] = vnetPrefix;
                    template.Parameters["LabSNetPrefix"] = snetPrefix;

                    var deployment = await azureDeploymentService
                        .DeployResourceGroupTemplateAsync(template, project.ResourceGroup.SubscriptionId, project.ResourceGroup.Name)
                        .ConfigureAwait(false);

                    return deployment.ResourceId;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectCreateActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
