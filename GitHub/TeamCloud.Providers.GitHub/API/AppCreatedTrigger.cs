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
using Flurl;
using Flurl.Http;
using TeamCloud.Providers.GitHub.Data;

namespace TeamCloud.Providers.GitHub
{
    public class AppCreatedTrigger
    {
        readonly GitHubService github;

        public AppCreatedTrigger(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(AppCreatedTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "created")] HttpRequestMessage httpRequest,
            ILogger log)
        {
            if (httpRequest is null)
                throw new ArgumentNullException(nameof(httpRequest));


            // json payload from the received webhook
            var query = httpRequest.RequestUri.ParseQueryString();
            var code = query["code"];
            // var installation_id = query["installation_id"];
            // var setup_action = query["setup_action"];

            Secrets.AppCode = code;

            log.LogWarning($"GitHub code: {code ?? "null"}");

            var url = $"https://api.github.com/app-manifests/{code}/conversions";

            var response = await url
                .WithHeader("User-Agent", $"TeamCloud/1.0.0.0")
                // .AllowAnyHttpStatus()
                .PostStringAsync("");

            var json = await response.Content.ReadAsStringAsync();

            var serializer = new Octokit.Internal.SimpleJsonSerializer();
            var app = serializer.Deserialize<GitHubAppCreated>(json);
            //log.LogWarning(app);


            Secrets.App = app;

            var html = $"<html><head><title>Test</title></head><body><p>GitHub app successfully created. Click <a href=\"https://github.com/apps/{app.Slug}/installations/new/permissions?target_id={app.Owner.Id}\">here</a> to install the app into your org and complete the setup.</p></body>";

            var result = new ContentResult
            {
                Content = html,
                ContentType = "text/html"
            };

            return result;
        }
    }
}
