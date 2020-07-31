using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class SynchronizeServiceConnectionsActivity
    {
        private readonly IAzureResourceService azureResourceService;
        private readonly IAuthenticationService authenticationService;
        private readonly IDistributedCache cache;

        public SynchronizeServiceConnectionsActivity(IAzureResourceService azureResourceService, IAuthenticationService authenticationService, IDistributedCache cache)
        {
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
                //var projectIdentityJson = await keyVault
                //    .GetSecretAsync(nameof(ProjectIdentity))
                //    .ConfigureAwait(false);

                //using var projectClient = await authenticationService
                //    .GetClientAsync<ProjectHttpClient>()
                //    .ConfigureAwait(false);

                //var azdoProjectId = await projectClient
                //    .GetProjectIdAsync(project, cache)
                //    .ConfigureAwait(false);

                //var azdoProject = await projectClient
                //    .GetProject(azdoProjectId.ToString())
                //    .ConfigureAwait(false);

                //using var endpointClient = await authenticationService
                //    .GetClientAsync<ServiceEndpointHttpClient>()
                //    .ConfigureAwait(false);

                //var endpoints = await endpointClient
                //    .GetServiceEndpointsByNamesAsync(azdoProjectId, new[] { "TeamCloud" })
                //    .ConfigureAwait(false);

                //var endpoint = endpoints
                //    .SingleOrDefault(ep => IsTeamCloudServiceEndpoint(ep));

                //if (endpoint is null)
                //{
                //    await endpointClient
                //        .CreateServiceEndpointAsync(new ServiceEndpoint()
                //        {
                //            Name = "TeamCloud",
                //            Type = ServiceEndpointTypes.AzureRM,
                //            Url = new Uri("https://management.azure.com/"),
                //            Data = new Dictionary<string, string>() {
                //                {"subscriptionId", "1272a66f-e2e8-4e88-ab43-487409186c3f" },
                //                {"subscriptionName", "subscriptionName" },
                //                {"environment", "AzureCloud"},
                //                {"scopeLevel", "Subscription"},
                //                {"creationMode", "Manual" }
                //            },
                //            Authorization = new EndpointAuthorization()
                //            {
                //                Scheme = EndpointAuthorizationSchemes.ServicePrincipal,
                //                Parameters = new Dictionary<string, string>()
                //                {
                //                    { "tenantid", "1272a66f-e2e8-4e88-ab43-487409186c3f" },
                //                    { "serviceprincipalid", "1272a66f-e2e8-4e88-ab43-487409186c3f" },
                //                    { "authenticationType", "spnKey" },
                //                    { "serviceprincipalkey", "SomePassword" }
                //                }
                //            },
                //            ServiceEndpointProjectReferences = new ServiceEndpointProjectReference[]
                //            {
                //                new ServiceEndpointProjectReference()
                //                {
                //                    Name = "",
                //                    Description = "",
                //                    ProjectReference = new ProjectReference()
                //                    {
                //                        Id = azdoProject.Id,
                //                        Name = azdoProject.Name
                //                    }
                //                }
                //            }
                //        })
                //        .ConfigureAwait(false);
                //}

                //static bool IsTeamCloudServiceEndpoint(ServiceEndpoint ep)
                //    => ep.Type.Equals(ServiceEndpointTypes.AzureRM)
                //    && ep.Name.Equals("TeamCloud")
                //    && ep.Authorization.Parameters.TryGetValue("serviceprincipalid", out var servicePrincipalId)
                //    && servicePrincipalId.Equals("", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exc)
            {
                throw exc.AsSerializable();
            }

        }
    }
}
