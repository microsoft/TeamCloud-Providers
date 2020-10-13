/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core.Model;
using TeamCloud.Serialization;
using TeamCloud.Providers.GitHub.Repos.Activities;

namespace TeamCloud.Providers.GitHub.Repos.Orchestrations
{
    public static class ComponentCreateOrchestration
    {
        [FunctionName(nameof(ComponentCreateOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var commandContext = functionContext.GetInput<ProviderCommandContext>();
            var command = (ProviderComponentCreateCommand)commandContext.Command;

            var commandResult = command.CreateResult();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            using (log.BeginCommandScope(command))
            {
                try
                {
                    functionContext.SetCustomStatus("Creating component", commandLog);

                    commandResult.Result = await functionContext
                        .CreateComponentAsync(command)
                        .ConfigureAwait(true);
                }
                catch (Exception exc)
                {
                    commandResult ??= command.CreateResult();
                    commandResult.Errors.Add(exc);

                    throw exc.AsSerializable();
                }
                finally
                {
                    var commandException = commandResult.Errors?.ToException();

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
