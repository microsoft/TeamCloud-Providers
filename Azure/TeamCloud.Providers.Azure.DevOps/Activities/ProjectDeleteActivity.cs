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
using TeamCloud.Orchestration;
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

        [FunctionName(nameof(ProjectDeleteActivity)), RetryOptions(5)]
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
                    using var projectClient = await authenticationService
                        .GetClientAsync<ProjectHttpClient>()
                        .ConfigureAwait(false);

                    var projectInstance = await projectClient
                        .GetProject(project.Name)
                        .ConfigureAwait(false);

                    if (projectInstance.State == ProjectState.WellFormed)
                    {
                        var projectOperation = await projectClient
                            .QueueDeleteProject(projectInstance.Id)
                            .ConfigureAwait(false);

                        return projectOperation.Id.ToString();
                    }
                    else if (projectInstance.State == ProjectState.Deleted)
                    {
                        return null;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Project {project.Name} is in state {projectInstance.State} and can't be deleted.");
                    }
                }
                catch (ProjectDoesNotExistException)
                {
                    log.LogWarning($"Could not find project by name or id '{project.Name}'");

                    return null;
                }
                catch (ProjectDoesNotExistWithNameException)
                {
                    log.LogWarning($"Could not find project by name or id '{project.Name}'");

                    return null;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectDeleteActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }

        }
    }
}
