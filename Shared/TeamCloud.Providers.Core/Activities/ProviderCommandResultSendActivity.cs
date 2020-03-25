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
        [FunctionName(nameof(ProviderCommandResultSendActivity))]
        [RetryOptions(5)]
        public static async Task RunActivity([ActivityTrigger] IDurableActivityContext functionContext, ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (commandResult, callbackUrl) = functionContext.GetInput<(ICommandResult, string)>();

            try
            {
                log.LogInformation($"Sending command result ({commandResult.CommandId}) to {callbackUrl}: {JsonConvert.SerializeObject(commandResult)}");

                await callbackUrl
                    .PostJsonAsync(commandResult)
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException postException) when (postException.Call.HttpStatus == HttpStatusCode.Gone)
            {
                // the server told us that the corresponding orchestration
                // is gone (reached a final state) and sending another request
                // doesn't make sense. so lets break our retry loop by raising
                // an exception that terminates the retry loop using the retry handler.

                throw new RetryCanceledException($"Sending command result ({commandResult.CommandId}) failed: {postException.Message}", postException);
            }
            catch (FlurlHttpException postException) when (postException.Call.HttpStatus == HttpStatusCode.Unauthorized)
            {
                // seems like the authentication token in the callback url is
                // no longer valid. there is no reason to give this operation
                // another shot. break the retry look by raising an exception
                // that breaks the retry logic using the retry handler.

                throw new RetryCanceledException($"Sending command result ({commandResult.CommandId}) failed: {postException.Message}", postException);
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableExc))
            {
                throw serializableExc;
            }
        }
    }
}
