/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public static class ProjectUpdateActivity
    {
        [FunctionName(nameof(ProjectUpdateActivity))]
        public static async Task<Project> RunActivity(
            [ActivityTrigger] ProjectUpdateCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            await Task.Delay(30 * 1000);

            return command.Payload;
        }
    }
}
