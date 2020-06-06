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
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class ProjectDeleteActivity
    {
        private readonly IAuthenticationService authenticationService;

        public ProjectDeleteActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(ProjectDeleteActivity))]
        public async Task<string> RunActivityAsync(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            using (log.BeginProjectScope(project))
            {
                try
                {
                    var connection = await authenticationService
                        .GetConnectionAsync()
                        .ConfigureAwait(false);

                    var projectClient = await connection
                        .GetClientAsync<ProjectHttpClient>()
                        .ConfigureAwait(false);

                    var projectInstance = await projectClient
                        .GetProject(project.Name)
                        .ConfigureAwait(false);

                    if (projectInstance is null)
                        return null;

                    var projectOperation = await projectClient
                        .QueueDeleteProject(projectInstance.Id)
                        .ConfigureAwait(false);

                    return projectOperation.Id.ToString();
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
