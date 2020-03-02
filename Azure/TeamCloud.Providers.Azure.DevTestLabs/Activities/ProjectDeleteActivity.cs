/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public static class ProjectDeleteActivity
    {
        [FunctionName(nameof(ProjectDeleteActivity))]
        public static async Task<Dictionary<string, string>> RunActivity(
            [ActivityTrigger] ProviderProjectDeleteCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            return new Dictionary<string, string>();
        }
    }
}
