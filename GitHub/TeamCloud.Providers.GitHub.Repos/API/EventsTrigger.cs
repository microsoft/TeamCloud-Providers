/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TeamCloud.Providers.GitHub.Repos.Services;

namespace TeamCloud.Providers.GitHub.Repos
{
    public class EventsTrigger
    {
        readonly GitHubService github;

        public EventsTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(EventsTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            // json payload from the received webhook
            var eventType = httpRequest.GitHubEventType();
            var payload = await httpRequest.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            await github.HandleWebhookAsync(eventType, payload, log)
                .ConfigureAwait(false);

            return new OkResult();
        }
    }
}
