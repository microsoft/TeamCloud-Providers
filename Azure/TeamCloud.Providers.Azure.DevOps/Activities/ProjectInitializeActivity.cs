/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class ProjectInitializeActivity
    {
        private readonly IAuthenticationService authenticationService;
        private readonly IDistributedCache cache;

        public ProjectInitializeActivity(IAuthenticationService authenticationService, IDistributedCache cache)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [FunctionName(nameof(ProjectInitializeActivity)), RetryOptions(3, FirstRetryInterval = "00:02:00")]
        public async Task RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            using (log.BeginProjectScope(project))
            {
                try
                {
                    using var projectClient = await authenticationService
                        .GetClientAsync<ProjectHttpClient>()
                        .ConfigureAwait(false);

                    var page = await projectClient
                        .GetProjects(ProjectState.WellFormed)
                        .ConfigureAwait(false);

                    var azdoProject = await page
                        .AsContinousEnumerationAsync((continuationToken) => projectClient.GetProjects(ProjectState.WellFormed, continuationToken: continuationToken))
                        .SingleAwaitAsync(p => new ValueTask<bool>(p.Name.Equals(project.Name)))
                        .ConfigureAwait(false);

                    var azdoProjectProperties = await projectClient
                        .GetProjectPropertiesAsync(azdoProject.Id, new string[] { GlobalConstants.TeamCloudProjectIdPropertyKey })
                        .ConfigureAwait(false);

                    var projectId = azdoProjectProperties
                        .SingleOrDefault(p => p.Name.Equals(GlobalConstants.TeamCloudProjectIdPropertyKey))?
                        .Value as string;

                    if (string.IsNullOrEmpty(projectId))
                    {
                        var propertyPatch = new JsonPatchDocument
                        {
                            new JsonPatchOperation()
                            {
                                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                                Path = $"/{GlobalConstants.TeamCloudProjectIdPropertyKey}",
                                Value = project.Id
                            },
                            new JsonPatchOperation()
                            {
                                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                                Path = $"/{GlobalConstants.TeamCloudProjectNamePropertyKey}",
                                Value = project.Name
                            }
                        };

                        await projectClient
                            .SetProjectPropertiesAsync(azdoProject.Id, propertyPatch)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectCreateActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
