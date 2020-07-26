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
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
{
    public static class UserSyncOrchestration
    {
        [FunctionName(nameof(UserSyncOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var project = functionContext
                .GetInput<Project>();

            try
            {
                await Task
                    .WhenAll(project.Users.Select(user => functionContext.CallActivityWithRetryAsync(nameof(UserRegisterActivity), user)))
                    .ConfigureAwait(true);


            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Failed to synchronize users for project {project.Id}: {exc.Message}");

                throw exc.AsSerializable();
            }
        }

    }
}
