/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Providers.GitHub.Repos.Services;

namespace TeamCloud.Providers.GitHub.Repos.Activities
{
    public class OfferSetActivity
    {
        private readonly GitHubService github;
        private readonly IAzureSessionService azureSessionService;

        public OfferSetActivity(GitHubService github, IAzureSessionService azureSessionService)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(OfferSetActivity))]
        public async Task<ComponentOffer> RunActivityAsync(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            if (activityContext is null)
                throw new ArgumentNullException(nameof(activityContext));

            var functionInput = activityContext.GetInput<Input>();

            var command = functionInput.Command;

            using (log.BeginCommandScope(command))
            {
                try
                {
                    var offer = await command.Links.Offers
                        .SetAsync(azureSessionService, functionInput.Offer)
                        .ConfigureAwait(false);

                    return offer;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(OfferSetActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }

        internal struct Input
        {
            public IProviderCommand Command { get; set; }

            public ComponentOffer Offer { get; set; }
        }
    }
}
