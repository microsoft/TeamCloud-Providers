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

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public static class ProjectCreateActivity
    {
        [FunctionName(nameof(ProjectCreateActivity))]
        public static async Task<Project> RunActivity(
            [ActivityTrigger] ProjectCreateCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            await Task.Delay(30 * 1000);

            return command.Payload;
        }
    }
}
