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

namespace TeamCloud.Providers.Azure.DevTestLabs.Orchestrations
{
    public static class ProviderEventOrchestration
    {
        [FunctionName(nameof(ProviderEventOrchestration))]
        public static Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var command = functionContext.GetInput<ProviderEventCommand>();
            var commandLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            return Task.CompletedTask;
        }
    }
}
