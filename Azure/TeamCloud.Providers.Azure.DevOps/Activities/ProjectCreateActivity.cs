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
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class ProjectCreateActivity
    {
        private readonly IAuthenticationService authenticationService;

        public ProjectCreateActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(ProjectCreateActivity)), RetryOptions(3, FirstRetryInterval = "00:02:00")]
        public async Task<string> RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            using (log.BeginProjectScope(project))
            {
                try
                {
                    using var processClient = await authenticationService
                        .GetClientAsync<ProcessHttpClient>()
                        .ConfigureAwait(false);

                    var processTemplates = await processClient
                        .GetProcessesAsync()
                        .ConfigureAwait(false);

                    var processCapabilities = new Dictionary<string, string>()
                    {
                        { TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityTemplateTypeIdAttributeName, processTemplates.Single(pt => pt.Name.Equals("Agile", StringComparison.OrdinalIgnoreCase)).Id.ToString() }
                    };

                    var versionControlCapabilities = new Dictionary<string, string>()
                    {
                        { TeamProjectCapabilitiesConstants.VersionControlCapabilityAttributeName, SourceControlTypes.Git.ToString() }
                    };

                    var projectTemplate = new TeamProject()
                    {
                        Name = project.Name,
                        Description = string.Empty,
                        Capabilities = new Dictionary<string, Dictionary<string, string>>()
                        {
                            { TeamProjectCapabilitiesConstants.VersionControlCapabilityName, versionControlCapabilities },
                            { TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityName, processCapabilities }
                        }
                    };

                    using var projectClient = await authenticationService
                        .GetClientAsync<ProjectHttpClient>()
                        .ConfigureAwait(false);

                    var projectOperation = await projectClient
                        .QueueCreateProject(projectTemplate)
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
