/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Orchestration.Deployment;
using TeamCloud.Providers.Azure.AppInsights.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
{
    public static class ProjectCreateOrchestration
    {
        [FunctionName(nameof(ProjectCreateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProviderProjectCreateCommand>();
            var commandResult = command.CreateResult();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            using (log.BeginCommandScope(command))
            {
                try
                {
                    functionContext.SetCustomStatus("Deploy resources", commandLog);

                    var deploymentOutput = await functionContext
                        .GetDeploymentOutputAsync(nameof(ProjectCreateActivity), command.Payload)
                        .ConfigureAwait(true);

                    if (deploymentOutput.TryGetValue("resourceId", out var resourceId))
                    {
                        functionContext.SetCustomStatus("Updating user permissions", commandLog);

                        await functionContext
                            .CallActivityWithRetryAsync(nameof(ProjectUsersActivity), (command.Payload, resourceId?.ToString()))
                            .ConfigureAwait(true);
                    }

                    commandResult.Result = new ProviderOutput
                    {
                        Properties = deploymentOutput.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString())
                    };
                }
                catch (Exception exc)
                {
                    commandResult ??= command.CreateResult();
                    commandResult.Errors.Add(exc);

                    throw exc.AsSerializable();
                }
                finally
                {
                    var commandException = commandResult.GetException();

                    if (commandException is null)
                        functionContext.SetCustomStatus($"Command succeeded", commandLog);
                    else
                        functionContext.SetCustomStatus($"Command failed: {commandException.Message}", commandLog, commandException);

                    functionContext.SetOutput(commandResult);
                }
            }
        }
    }
}
