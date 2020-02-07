/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.DevTestLabs.Activities;

namespace TeamCloud.Providers.Azure.DevTestLabs.Orchestrations
{
    public static class ProjectUpdateOrchestration
    {
        [FunctionName(nameof(ProjectUpdateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProjectUpdateCommand>();

            var project = await functionContext
                .CallActivityAsync<Project>(nameof(ProjectUpdateActivity), command)
                .ConfigureAwait(true);

            var commandResult = command.CreateResult();
            commandResult.Result = project;

            functionContext.SetOutput(commandResult);
        }
    }
}
