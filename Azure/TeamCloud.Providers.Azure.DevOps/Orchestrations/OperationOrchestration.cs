/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.Services.Operations;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Activities;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Orchestrations
{
    public static class OperationOrchestration
    {
        [FunctionName(nameof(OperationOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            (string operationActivityName, object operationActivityInput, string operationInstanceId)
                = functionContext.GetInput<(string, object, string)>();

            var operationLog = functionContext.CreateReplaySafeLogger(log ?? NullLogger.Instance);

            try
            {
                if (string.IsNullOrEmpty(operationInstanceId))
                {
                    operationInstanceId = await functionContext
                            .CallActivityWithRetryAsync<string>(operationActivityName, operationActivityInput)
                            .ConfigureAwait(true);

                    if (!string.IsNullOrEmpty(operationInstanceId))
                        functionContext.ContinueAsNew((operationActivityName, operationActivityInput, operationInstanceId));
                }
                else
                {
                    await functionContext
                        .CreateTimer(functionContext.CurrentUtcDateTime.AddSeconds(10), CancellationToken.None)
                        .ConfigureAwait(true);

                    var status = await functionContext
                        .CallActivityWithRetryAsync<OperationStatus>(nameof(OperationStatusActivity), operationInstanceId)
                        .ConfigureAwait(true);

                    if (status.IsProgressStatus())
                    {
                        functionContext
                            .ContinueAsNew((operationActivityName, operationActivityInput, operationInstanceId));
                    }
                    else if (status.IsErrorStatus())
                    {
                        var operationError = await functionContext
                            .CallActivityWithRetryAsync<string>(nameof(OperationErrorActivity), operationInstanceId)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(operationError))
                            throw new Exception(operationError);
                    }
                }
            }
            catch (Exception exc)
            {
                operationLog.LogError(exc, $"Orchestration '{nameof(OperationOrchestration)}' failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
