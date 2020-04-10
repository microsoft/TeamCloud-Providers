/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.DevTestLabs.Activities;
using TeamCloud.Providers.Core;

namespace TeamCloud.Providers.Azure.DevTestLabs.Orchestrations
{
    public static class ProjectCreateOrchestration
    {
        [FunctionName(nameof(ProjectCreateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProviderProjectCreateCommand>();
            var commandResult = command.CreateResult();

            var deploymentOutput = await functionContext
                .GetDeploymentOutputAsync(nameof(ProjectCreateActivity), command.Payload)
                .ConfigureAwait(true);

            commandResult.Result = new ProviderOutput
            {
                Properties = deploymentOutput.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString())
            };

            functionContext.SetOutput(commandResult);
        }
    }
}
