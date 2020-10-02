/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Orchestration;
using TeamCloud.Serialization;
using TeamCloud.Providers.GitHub.Actions.Services;
using TeamCloud.Providers.Core.Model;
using System.Linq;

namespace TeamCloud.Providers.GitHub.Actions.Activities
{
    public class CommandDispatchActivity
    {
        private readonly GitHubService github;

        public CommandDispatchActivity(GitHubService github)
        {
            this.github = github ?? throw new ArgumentNullException(nameof(github));
        }

        [FunctionName(nameof(CommandDispatchActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var commandContext = functionContext.GetInput<ProviderCommandContext>();
            var command = commandContext.Command;

            try
            {
                var repo = commandContext.Context.GetValues("repo").FirstOrDefault();

                if (string.IsNullOrEmpty(repo))
                    throw new InvalidOperationException("Parameter 'repo' is required in the query string.");

                await github
                    .SendRepositoryEventAsync(repo, command)
                    .ConfigureAwait(false);

                log.LogInformation($"Dispatched workflow.");
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"{nameof(CommandDispatchActivity)} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
