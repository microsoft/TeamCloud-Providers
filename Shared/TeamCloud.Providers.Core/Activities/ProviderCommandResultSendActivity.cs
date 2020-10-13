/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Core.Activities
{
    public static class ProviderCommandResultSendActivity
    {
        [FunctionName(nameof(ProviderCommandResultSendActivity)), RetryOptions(5, FirstRetryInterval = "00:01:00")]
        public static async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (commandResult, callbackUrl) = functionContext.GetInput<(ICommandResult, string)>();

            try
            {
                log.LogInformation($"Sending command result ({commandResult.CommandId}) to {callbackUrl}:\n{JsonConvert.SerializeObject(commandResult, Formatting.Indented)}");

                await callbackUrl
                    .PostJsonAsync(commandResult)
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException postException) when (postException.Call.HttpStatus == HttpStatusCode.Gone)
            {
                log.LogError(postException, $"Activity '{nameof(ProviderCommandResultSendActivity)}' failed: {postException.Message}");

                // the server told us that the corresponding orchestration
                // is gone (reached a final state) and sending another request
                // doesn't make sense. so lets break our retry loop by raising
                // an exception that terminates the retry loop using the retry handler.

                throw new RetryCanceledException($"Sending command result ({commandResult.CommandId}) failed: {postException.Message}", postException);
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Activity '{nameof(ProviderCommandResultSendActivity)}' failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
