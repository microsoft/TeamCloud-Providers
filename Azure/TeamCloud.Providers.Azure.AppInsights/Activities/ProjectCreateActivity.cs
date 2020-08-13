/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Deployment;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.AppInsights.Templates;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public class ProjectCreateActivity
    {
        private readonly IAzureDeploymentService azureDeploymentService;
        private readonly ILogger<ProjectCreateActivity> logger;

        public ProjectCreateActivity(IAzureDeploymentService azureDeploymentService, ILogger<ProjectCreateActivity> logger)
        {
            this.azureDeploymentService = azureDeploymentService ?? throw new ArgumentNullException(nameof(azureDeploymentService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [FunctionName(nameof(ProjectCreateActivity)), RetryOptions(3, FirstRetryInterval = "00:02:00")]
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
                    var template = new ProjectCreateTemplate();

                    template.Parameters["ProviderName"] = Assembly.GetExecutingAssembly().GetName().Name;
                    template.Parameters["ProjectName"] = project.Name;

                    var deployment = await azureDeploymentService
                        .DeployResourceGroupTemplateAsync(template, project.ResourceGroup.SubscriptionId, project.ResourceGroup.Name)
                        .ConfigureAwait(false);

                    log.LogInformation($"Created deployment {deployment.ResourceId} at {project.ResourceGroup.Id}");

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
