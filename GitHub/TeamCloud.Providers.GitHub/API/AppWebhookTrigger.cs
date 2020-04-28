/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using TeamCloud.Providers.GitHub.Data;

namespace TeamCloud.Providers.GitHub
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
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "events")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));


            // json payload from the received webhook
            var eventType = httpRequest.GitHubEventType();
            var payload = await httpRequest.Content.ReadAsStringAsync();

            var serializer = new SimpleJsonSerializer();

            await Handle(serializer, payload, eventType, log);

            log.LogWarning($"GitHub eventType:{eventType ?? "null"}\nGitHub payload:\n{payload ?? "null"}");


            return new OkResult();
            // return new OkObjectResult("hi");
        }

        private Task Handle(SimpleJsonSerializer serializer, string payload, string eventType, ILogger log)
            => eventType.ToLowerInvariant() switch
            {
                "ping" => HandlePing(serializer, payload, log),
                _ => HandleOther(serializer, payload, log)
            };

        private Task HandlePing(SimpleJsonSerializer serializer, string payload, ILogger log)
        {

            var ping = serializer.Deserialize<GitHubPing>(payload);

            return Task.FromResult(ping);
        }

        private Task HandleOther(SimpleJsonSerializer serializer, string payload, ILogger log)
        {
            var activity = serializer.Deserialize<ActivityPayload>(payload);


            if (Secrets.Installer is null && activity?.Sender != null)
            {
                Secrets.Installer = activity.Sender;
            }

            return Task.FromResult(activity);
        }
    }
}
