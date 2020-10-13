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
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.GitHub.Repos.Activities
{
    public class ProjectGetActivity
    {
        private readonly IAzureSessionService azureSessionService;

        public ProjectGetActivity(IAzureSessionService azureSessionService)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ProjectGetActivity))]
        public async Task<Project> RunActivity(
            [ActivityTrigger] IDurableActivityContext activityContext,
            ILogger log)
        {
            if (activityContext is null)
                throw new ArgumentNullException(nameof(activityContext));

            var functionInput = activityContext.GetInput<Input>();

            var command = functionInput.Command;
            var projectId = functionInput.ProjectId;

            try
            {
                return await command.Links.Project
                    .GetAsync<Project>(azureSessionService)
                    .ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"{nameof(ProjectGetActivity)} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }

        internal struct Input
        {
            public string ProjectId { get; set; }
            public IProviderCommand Command { get; set; }
        }
    }

    internal static class ProjectGetExtension
    {
        public static Task<Project> GetProjectAsync(this IDurableOrchestrationContext orchestrationContext, IProviderCommand command, string projectId)
            => orchestrationContext.CallActivityWithRetryAsync<Project>(nameof(ProjectGetActivity), new ProjectGetActivity.Input { Command = command, ProjectId = projectId });
    }
}
