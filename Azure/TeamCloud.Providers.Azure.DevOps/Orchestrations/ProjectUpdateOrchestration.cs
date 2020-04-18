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
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
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

            if (log is null)
                throw new ArgumentNullException(nameof(log));

            var command = functionContext.GetInput<ProviderProjectUpdateCommand>();
            var commandResult = command.CreateResult();

            using (log.BeginCommandScope(command))
            {
                try
                {
                    var properties = await functionContext
                        .CallActivityWithRetryAsync<Dictionary<string, string>>(nameof(ProjectUpdateActivity), command)
                        .ConfigureAwait(true);

                    commandResult.Result = new ProviderOutput { Properties = properties };
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
                        functionContext.SetCustomStatus($"Command succeeded", log);
                    else
                        functionContext.SetCustomStatus($"Command failed: {commandException.Message}", log, commandException);

                    functionContext.SetOutput(commandResult);
                }
            }
        }
    }
}
