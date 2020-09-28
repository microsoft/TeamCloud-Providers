/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TeamCloud.Providers.GitHub.Actions.Services;

namespace TeamCloud.Providers.GitHub.Actions
{
    public class AppWebhookTrigger
    {
        readonly GitHubService github;

        public AppWebhookTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(AppWebhookTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequestMessage httpRequest,
            [DurableClient] IDurableOrchestrationClient functionClient,
            ILogger log)
        {
            try
            {
                if (httpRequest is null)
                    throw new ArgumentNullException(nameof(httpRequest));

                if (functionClient is null)
                    throw new ArgumentNullException(nameof(functionClient));

                // json payload from the received webhook
                var eventType = httpRequest.GitHubEventType();
                var payload = await httpRequest.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);

                if (eventType.Equals("workflow_run", StringComparison.OrdinalIgnoreCase))
                {
                    // log.LogWarning("AppWebhookTrigger: Handeling webhook");
                    var commandResult = await github.HandleWorkflowRunAsync(payload, log)
                        .ConfigureAwait(false);

                    if (!(commandResult is null))
                    {
                        // log.LogWarning("AppWebhookTrigger: Raising event");
                        await functionClient
                            .RaiseEventAsync(commandResult.CommandId.ToString(), commandResult.CommandId.ToString(), commandResult)
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    await github.HandleWebhookAsync(eventType, payload, log)
                        .ConfigureAwait(false);
                }

                // log.LogWarning("AppWebhookTrigger: Returning inside try");

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogWarning($"Exception occored in AppWebhookTrigger: {ex.Message}");
                // swallow exceptions so that we always respond with okay to the github webhook
            }

            // log.LogWarning("AppWebhookTrigger: Returning outside try");
            return new OkResult();
        }
    }
}
