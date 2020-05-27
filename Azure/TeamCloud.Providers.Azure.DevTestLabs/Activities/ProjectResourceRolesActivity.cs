/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Model;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public class ProjectResourceRolesActivity
    {
        private readonly IAzureResourceService azureResourceService;

        public ProjectResourceRolesActivity(IAzureResourceService azureResourceService)
        {
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        [FunctionName(nameof(ProjectResourceRolesActivity))]
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
                    var roleAssignments = (project.Users ?? Enumerable.Empty<User>())
                        .ToDictionary(usr => usr.Id, usr => Enumerable.Repeat(GetRoleDefinitionId(usr, project.Id), 1));

                    if (roleAssignments.Any())
                    {
                        var resource = await azureResourceService
                            .GetResourceAsync(resourceId, throwIfNotExists: true)
                            .ConfigureAwait(false);

                        await resource
                            .SetRoleAssignmentsAsync(roleAssignments)
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectResourceRolesActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }

            static Guid GetRoleDefinitionId(User user, Guid projectId) => user.RoleFor(projectId) switch
            {
                ProjectUserRole.Owner => AzureRoleDefinition.Contributor,
                ProjectUserRole.Member => AzureRoleDefinition.DevTestLabUser,
                _ => throw new NotSupportedException($"User '{user.Id}' has an unsupported role '{user.RoleFor(projectId)}'")
            };
        }
    }
}
