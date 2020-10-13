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
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core.Model;
using TeamCloud.Serialization;
using TeamCloud.Providers.GitHub.Repos.Activities;
using System.Linq;

namespace TeamCloud.Providers.GitHub.Repos.Orchestrations
{
    public static class ProjectDeleteOrchestration
    {
        [FunctionName(nameof(ProjectDeleteOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var commandContext = functionContext.GetInput<ProviderCommandContext>();
            var command = (ProviderProjectDeleteCommand)commandContext.Command;

            var commandResult = command.CreateResult();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            using (log.BeginCommandScope(command))
            {
                try
                {
                    functionContext.SetCustomStatus("Deleting resources", commandLog);

                    await functionContext
                        .DeleteProjectAsync(command.Payload)
                        .ConfigureAwait(true);

                    functionContext.SetCustomStatus("Getting components", commandLog);

                    var components = await functionContext
                        .ListComponentsAsync(command)
                        .ConfigureAwait(true);

                    if (components.Any())
                    {
                        functionContext.SetCustomStatus("Deleting components", commandLog);

                        await Task
                            .WhenAll(components.Select(c => functionContext.DeleteComponentAsync(command, c)))
                            .ConfigureAwait(true);
                    }

                    commandResult.Result = new ProviderOutput();
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
