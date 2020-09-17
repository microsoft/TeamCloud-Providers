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
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Core.Model;

namespace TeamCloud.Providers.Azure.DevTestLabs.Orchestrations.Commands
{
    public static class ProviderEventCommandOrchestration
    {
        [FunctionName(nameof(ProviderEventCommandOrchestration))]
        public static Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var commandContext = functionContext.GetInput<ProviderCommandContext>();
            var command = (ProviderEventCommand)commandContext.Command;
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            return Task.CompletedTask;
        }
    }
}
