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
            [ActivityTrigger] ProviderRegisterCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            using (log.BeginCommandScope(command))
            {
                try
                {
                    var identity = await azureSessionService
                        .GetIdentityAsync()
                        .ConfigureAwait(false);

                    var registration = new ProviderRegistration
                    {
                        PrincipalId = identity?.ObjectId
                        // ,CommandMode = ProviderCommandMode.Extended
                    };

                    await github
                        .SendRepositoryEventAsync("workflow-provider", command, log)
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
