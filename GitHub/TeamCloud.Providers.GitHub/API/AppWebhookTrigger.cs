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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));

            // json payload from the received webhook
            var eventType = httpRequest.GitHubEventType();
            var payload = await httpRequest.Content.ReadAsStringAsync();

            await Handle(new SimpleJsonSerializer(), payload, eventType, log);

            return new OkResult();
        }

        private Task Handle(SimpleJsonSerializer serializer, string payload, string eventType, ILogger log)
            => eventType.ToLowerInvariant() switch
            {
                "ping" => HandlePing(serializer, payload, log),
                "installation" => HandleInstallation(serializer, payload, log),
                _ => HandleOther(serializer, eventType, payload, log)
            };

        private Task HandlePing(SimpleJsonSerializer serializer, string payload, ILogger log)
        {
            var ping = serializer.Deserialize<GitHubPing>(payload);

            log?.LogWarning($"Received GitHub Webhook: [ EventType: ping, Zen: {ping.Zen ?? "null"} ]");

            return Task.FromResult(ping);
        }

        private Task HandleInstallation(SimpleJsonSerializer serializer, string payload, ILogger log)
        {
            var hook = serializer.Deserialize<GitHubAppInstallationHook>(payload);

            Secrets.Installation = hook?.Installation;
            Secrets.Installer = hook?.Sender;

            log?.LogWarning($"Received GitHub Webhook: [ EventType: installation, Action: {hook.Action ?? "null"} ]");

            return Task.FromResult(hook.Installation);
        }

        private Task HandleOther(SimpleJsonSerializer serializer, string eventType, string payload, ILogger log)
        {
            var hook = serializer.Deserialize<GitHubHookPayload>(payload);

            log?.LogWarning($"Received GitHub Webhook: [ EventType: {eventType}, Action: {hook.Action ?? "null"} ]");

            return Task.FromResult(hook);
        }
    }
}
