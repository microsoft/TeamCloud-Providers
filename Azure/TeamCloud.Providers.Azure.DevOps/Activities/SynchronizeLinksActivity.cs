/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using TeamCloud.Azure;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public class SynchronizeLinksActivity
    {
        private readonly IAzureSessionService azureSessionService;
        private readonly IAuthenticationService authenticationService;

        public SynchronizeLinksActivity(IAzureSessionService azureSessionService, IAuthenticationService authenticationService)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(SynchronizeLinksActivity)), RetryOptions(3)]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var project = functionContext.GetInput<Project>();

            using (log.BeginProjectScope(project))
            {
                try
                {
                    using var projectClient = await authenticationService
                        .GetClientAsync<ProjectHttpClient>()
                        .ConfigureAwait(false);

                    var projectInstance = await projectClient
                        .GetProject(project.Name)
                        .ConfigureAwait(false);

                    if (projectInstance.Links.Links.TryGetValue("web", out var value)
                        && value is Microsoft.VisualStudio.Services.WebApi.ReferenceLink link)
                    {
                        var projectLink = new ProjectLink()
                        {
                            HRef = link.Href,
                            Title = $"Azure DevOps ({projectInstance.Name})".Trim(),
                            Type = ProjectLinkType.AzureResource

                        }.WithGeneratedId(projectInstance.Id.ToString());

                        _ = await project.Links.Links
                            .SetAsync(azureSessionService, projectLink)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    log.LogWarning(exc, $"Synchronizing links for project {project.Id} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
