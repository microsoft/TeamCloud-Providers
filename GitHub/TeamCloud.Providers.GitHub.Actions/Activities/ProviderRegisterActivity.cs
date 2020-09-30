/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Core.Model;
using TeamCloud.Providers.GitHub.Actions.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.GitHub.Actions.Activities
{
    public class ProviderRegisterActivity
    {
        private readonly GitHubService github;
        private readonly IAzureSessionService azureSessionService;

        public ProviderRegisterActivity(IAzureSessionService azureSessionService, GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ProviderRegisterActivity))]
        public async Task<ProviderRegistration> RunActivityAsync(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var commandContext = functionContext.GetInput<ProviderCommandContext>();
            var command = commandContext.Command;

            using (log.BeginCommandScope(command))
            {
                try
                {
                    var repo = commandContext.Context.GetValues("repo").FirstOrDefault();

                    if (string.IsNullOrEmpty(repo))
                        throw new InvalidOperationException("Parameter 'repo' is required in the query string.");

                    var identity = await azureSessionService
                        .GetIdentityAsync()
                        .ConfigureAwait(false);

                    var registration = new ProviderRegistration
                    {
                        PrincipalId = identity?.ObjectId
                        // ,CommandMode = ProviderCommandMode.Extended
                    };

                    await github
                        .SendRepositoryEventAsync(repo, command, log)
                        .ConfigureAwait(false);

                    return registration;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProviderRegisterActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
