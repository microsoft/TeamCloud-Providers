/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.ServiceEndpoints;
using Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi;
using TeamCloud.Azure;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class SynchronizeServiceConnectionsActivity
    {
        private readonly IAzureSessionService azureSessionService;
        private readonly IAzureResourceService azureResourceService;
        private readonly IAuthenticationService authenticationService;
        private readonly IDistributedCache cache;

        public SynchronizeServiceConnectionsActivity(IAzureSessionService azureSessionService, IAzureResourceService azureResourceService, IAuthenticationService authenticationService, IDistributedCache cache)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [FunctionName(nameof(SynchronizeServiceConnectionsActivity))]
        public async Task RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            try
            {
                var projectIdentity = await project.Links.Identity
                    .GetAsync<ProjectIdentity>(azureSessionService)
                    .ConfigureAwait(false);

                if (projectIdentity != null)
                {
                    using var projectClient = await authenticationService
                        .GetClientAsync<ProjectHttpClient>()
                        .ConfigureAwait(false);

                    var azdoProjectId = await projectClient
                        .GetProjectIdAsync(project, cache)
                        .ConfigureAwait(false);

                    var azdoProject = await projectClient
                        .GetProject(azdoProjectId.ToString())
                        .ConfigureAwait(false);

                    using var endpointClient = await authenticationService
                        .GetClientAsync<ServiceEndpointHttpClient>()
                        .ConfigureAwait(false);

                    var endpoints = await endpointClient
                        .GetServiceEndpointsByNamesAsync(azdoProjectId, new[] { $"TeamCloud {project.Id}" })
                        .ConfigureAwait(false);

                    var endpoint = endpoints
                        .SingleOrDefault(ep => IsTeamCloudServiceEndpoint(ep));

                    if (endpoint is null)
                    {
                        await endpointClient
                            .CreateServiceEndpointAsync(new Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ServiceEndpoint()
                            {
                                Name = $"TeamCloud {project.Id}",
                                Type = ServiceEndpointTypes.AzureRM,
                                Url = new Uri("https://management.azure.com/"),
                                Data = new Dictionary<string, string>() {
                                    {"subscriptionId", project.ResourceGroup.SubscriptionId.ToString() },
                                    {"subscriptionName", project.ResourceGroup.SubscriptionId.ToString() },
                                    {"environment", "AzureCloud"},
                                    {"scopeLevel", "Subscription"},
                                    {"creationMode", "Manual" }
                                },
                                Authorization = new EndpointAuthorization()
                                {
                                    Scheme = EndpointAuthorizationSchemes.ServicePrincipal,
                                    Parameters = new Dictionary<string, string>()
                                    {
                                        { "tenantid", projectIdentity.TenantId.ToString() },
                                        { "serviceprincipalid", projectIdentity.ApplicationId.ToString() },
                                        { "authenticationType", "spnKey" },
                                        { "serviceprincipalkey", projectIdentity.Secret }
                                    }
                                },
                                ServiceEndpointProjectReferences = new ServiceEndpointProjectReference[]
                                {
                                    new ServiceEndpointProjectReference()
                                    {
                                        Name = "TeamCloud",
                                        Description = "TeamCloud Project Identity",
                                        ProjectReference = new ProjectReference()
                                        {
                                            Id = azdoProject.Id,
                                            Name = azdoProject.Name
                                        }
                                    }
                                }
                            })
                            .ConfigureAwait(false);
                    }

                    static bool IsTeamCloudServiceEndpoint(Microsoft.VisualStudio.Services.ServiceEndpoints.WebApi.ServiceEndpoint ep)
                        => ep.Type.Equals(ServiceEndpointTypes.AzureRM, StringComparison.OrdinalIgnoreCase)
                        && ep.Name.Equals("TeamCloud", StringComparison.OrdinalIgnoreCase)
                        && ep.Authorization.Scheme.Equals(EndpointAuthorizationSchemes.ServicePrincipal, StringComparison.OrdinalIgnoreCase);
                }

            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Synchronizing service connections for project {project.Id} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
