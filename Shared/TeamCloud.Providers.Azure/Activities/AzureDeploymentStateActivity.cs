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
using TeamCloud.Orchestration;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.Activities
{
    public class AzureDeploymentStateActivity
    {
        private readonly IAzureDeploymentService azureDeploymentService;

        public AzureDeploymentStateActivity(IAzureDeploymentService azureDeploymentService)
        {
            this.azureDeploymentService = azureDeploymentService ?? throw new ArgumentNullException(nameof(azureDeploymentService));
        }

        [FunctionName(nameof(AzureDeploymentStateActivity))]
        [RetryOptions(5)]
        public async Task<AzureDeploymentState> RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var resourceId = functionContext.GetInput<string>();

            try
            {
                var deployment = await azureDeploymentService
                    .GetAzureDeploymentAsync(resourceId)
                    .ConfigureAwait(false);

                var deploymentState = await deployment
                    .GetStateAsync()
                    .ConfigureAwait(false);

                log.LogInformation($"Deployment '{resourceId}' is in state '{deploymentState}'");

                return deploymentState;
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Activity {nameof(AzureDeploymentStateActivity)} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }

}
