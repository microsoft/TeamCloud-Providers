/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.DevTestLabs.Activities;

namespace TeamCloud.Providers.Azure.DevTestLabs.Orchestrations
{
    public static class SendCommandResultOrchestration
    {

        [FunctionName(nameof(SendCommandResultOrchestration))]
        public static async Task RunOrchestration(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var request = functionContext.GetInput<Request>();

            var result = await functionContext
                .CallActivityAsync<ICommandResult>(nameof(SendCommandResultActivity), request.InstanceId)
                .ConfigureAwait(true);

            if (result is null)
            {
                await functionContext
                    .CreateTimer(functionContext.CurrentUtcDateTime.AddSeconds(1), CancellationToken.None)
                    .ConfigureAwait(true);

                functionContext.ContinueAsNew(request);
            }
            else
            {
                var response = await functionContext
                    .CallHttpAsync(HttpMethod.Post, new Uri(request.CallbackUrl), JsonConvert.SerializeObject(result))
                    .ConfigureAwait(true);
            }
        }

        public class Request
        {
            public string InstanceId { get; set; }

            public string CallbackUrl { get; set; }
        }
    }
}