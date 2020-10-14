/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Serialization;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Model.Commands;
using TeamCloud.Azure;
using System.Collections.Generic;

namespace TeamCloud.Providers.GitHub.Repos.Activities
{
    public class ComponentListActivity
    {
        private readonly IAzureSessionService azureSessionService;

        public ComponentListActivity(IAzureSessionService azureSessionService)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ComponentListActivity)), RetryOptions(10, FirstRetryInterval = "00:02:00")]
        public async Task<List<Component>> RunActivity(
            [ActivityTrigger] IProviderCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var project = command.Payload;

            using (log.BeginCommandScope(command))
            {
                try
                {
                    if (command.Links.ProjectComponents is null)
                        return new List<Component>();

                    return await command.Links.ProjectComponents
                        .GetAsync<List<Component>>(azureSessionService)
                        .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ComponentListActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }

    internal static class ComponentListExtentions
    {
        public static Task<List<Component>> ListComponentsAsync(this IDurableOrchestrationContext orchestrationContext, IProviderCommand command)
            => orchestrationContext.CallActivityWithRetryAsync<List<Component>>(nameof(ComponentListActivity), command);
    }
}
