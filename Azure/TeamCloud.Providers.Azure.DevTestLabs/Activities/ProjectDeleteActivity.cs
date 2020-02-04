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

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public static class ProjectDeleteActivity
    {
        [FunctionName(nameof(ProjectDeleteActivity))]
        public static async Task<Project> RunActivity(
            [ActivityTrigger] ProjectDeleteCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            await Task.Delay(30 * 1000);

            return command.Payload;
        }
    }
}
