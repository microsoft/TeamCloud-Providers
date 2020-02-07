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
using TeamCloud.Providers.Azure.DevOps.Activities;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
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

            var command = functionContext.GetInput<ProjectCreateCommand>();

            var result = await functionContext
                .CallActivityAsync<Project>(nameof(ProjectCreateActivity), command)
                .ConfigureAwait(true);

            var commandResult = command.CreateResult();

            commandResult.Result = result;

            functionContext.SetOutput(commandResult);
        }
    }
}
