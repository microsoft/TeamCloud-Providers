using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure;
using TeamCloud.Azure.Resources;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core.Clients;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public class ProjectResourceLinksActivity
    {
        private readonly IAzureSessionService azureSessionService;

        public ProjectResourceLinksActivity(IAzureSessionService azureSessionService)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ProjectResourceLinksActivity)), RetryOptions(3)]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            var (project, resourceId) = functionContext.GetInput<(Project, string)>();

            using (log.BeginProjectScope(project))
            {
                try
                {
                    if (AzureResourceIdentifier.TryParse(resourceId, out var azureResourceIdentifier))
                    {
                        var identity = await azureSessionService
                            .GetIdentityAsync()
                            .ConfigureAwait(false);

                        var projectLink = new ProjectLink()
                        {
                            HRef = azureResourceIdentifier.GetPortalUrl(identity.TenantId),
                            Title = $"DevTest Lab ({azureResourceIdentifier.ResourceTypes.Last().Value})".Trim()

                        }.WithGeneratedId(resourceId);

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
                    log.LogWarning(exc, $"{nameof(ProjectResourceLinksActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
