/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamCloud.Azure.Deployment;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.Orchestrations
{
    public static class AzureDeploymentOrchestration
    {
        [FunctionName(nameof(AzureDeploymentOrchestration))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (deploymentActivityName, deploymentActivityInput, deploymentResourceId, deploymentDelete) = functionContext.GetInput<(string, object, string, bool)>();
            var deploymentLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            IReadOnlyDictionary<string, object> deploymentOutput = null;

            try
            {
                if (string.IsNullOrEmpty(deploymentResourceId))
                {
                    functionContext.SetCustomStatus($"Starting deployment using activity '{deploymentActivityName}'", log);

                    deploymentResourceId = await functionContext
                        .CallActivityWithRetryAsync<string>(deploymentActivityName, deploymentActivityInput)
                        .ConfigureAwait(true);

                    if (!string.IsNullOrEmpty(deploymentResourceId))
                    {
                        functionContext.SetCustomStatus($"Monitoring deployment '{deploymentResourceId}'", log);

                        functionContext.ContinueAsNew((deploymentActivityName, deploymentActivityInput, deploymentResourceId, false));
                    }
                }
                else if (deploymentDelete)
                {
                    var state = await functionContext
                        .CallActivityWithRetryAsync<AzureDeploymentState>(nameof(AzureDeploymentStateActivity), deploymentResourceId)
                        .ConfigureAwait(true);

                    if (state.IsErrorState())
                    {
                        // deployments ended up in an error state will stay 
                        // alive for seven days to investigate the issue

                        var schedule = functionContext.CurrentUtcDateTime.AddDays(7).Date;

                        functionContext.SetCustomStatus($"Deployment delete scheduled for '{schedule}'", log);

                        await functionContext
                            .CreateTimer(schedule, CancellationToken.None)
                            .ConfigureAwait(true);
                    }

                    functionContext.SetCustomStatus($"Deleting deployment '{deploymentResourceId}'", log);

                    await functionContext
                        .CallActivityWithRetryAsync(nameof(AzureDeploymentDeleteActivity), deploymentResourceId)
                        .ConfigureAwait(true);
                }
                else
                {
                    await functionContext
                        .CreateTimer(functionContext.CurrentUtcDateTime.AddSeconds(30), CancellationToken.None)
                        .ConfigureAwait(true);

                    var state = await functionContext
                        .CallActivityWithRetryAsync<AzureDeploymentState>(nameof(AzureDeploymentStateActivity), deploymentResourceId)
                        .ConfigureAwait(true);

                    functionContext.SetCustomStatus($"Deployment state '{deploymentResourceId}': {state}", log);

                    if (state.IsProgressState())
                    {
                        functionContext.ContinueAsNew((deploymentActivityName, deploymentActivityInput, deploymentResourceId));
                    }
                    else
                    {
                        try
                        {
                            if (state.IsErrorState())
                            {
                                var errors = await functionContext
                                    .CallActivityWithRetryAsync<IEnumerable<string>>(nameof(AzureDeploymentErrorsActivity), deploymentResourceId)
                                    .ConfigureAwait(true);

                                throw new AzureDeploymentException($"Deployment '{deploymentResourceId}' failed", deploymentResourceId, errors?.ToArray() ?? Array.Empty<string>());
                            }
                            else
                            {
                                deploymentOutput = await functionContext
                                    .CallActivityWithRetryAsync<IReadOnlyDictionary<string, object>>(nameof(AzureDeploymentOutputActivity), deploymentResourceId)
                                    .ConfigureAwait(true);
                            }
                        }
                        finally
                        {
                            functionContext.SetCustomStatus($"Initiate deployment clean up for '{deploymentResourceId}'", log);

                            functionContext.StartNewOrchestration(nameof(AzureDeploymentOrchestration), (deploymentActivityName, deploymentActivityInput, deploymentResourceId, true));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                deploymentLog.LogError(exc, $"Orchestration '{nameof(AzureDeploymentOrchestration)}' failed: {exc.Message}");

                throw exc.AsSerializable();
            }
            finally
            {
                functionContext.SetOutput(deploymentOutput);
            }
        }
    }
}
