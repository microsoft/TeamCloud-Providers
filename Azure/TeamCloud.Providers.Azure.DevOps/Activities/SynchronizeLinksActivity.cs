/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
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
                            Title = $"Azure DevOps ({projectInstance.Name})".Trim()

                        }.WithGeneratedId(projectInstance.Id.ToString());

                        try
                        {
                            _ = await project.Links.Links
                                .PutAsync(azureSessionService, projectLink)
                                .ConfigureAwait(false);
                        }
                        catch (FlurlHttpException exc) when (exc.Call.HttpStatus == System.Net.HttpStatusCode.NotFound)
                        {
                            _ = await project.Links.Links
                                .PostAsync(azureSessionService, projectLink)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception exc)
                {
                    log.LogWarning(exc, $"{nameof(SynchronizeLinksActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
