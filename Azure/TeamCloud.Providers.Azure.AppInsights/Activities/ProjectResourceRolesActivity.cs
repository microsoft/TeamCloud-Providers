/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
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
            [ActivityTrigger] IDurableActivityContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            try
            {
                var (project, resourceId) = functionContext.GetInput<(Project, string)>();

                var roleAssignments = (project.Users ?? Enumerable.Empty<User>())
                    .ToDictionary(usr => usr.Id, usr => Enumerable.Repeat(GetRoleDefinitionId(usr), 1));

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
            catch (Exception exc) when (!exc.IsSerializable(out var serializableException))
            {
                throw serializableException;
            }

            static Guid GetRoleDefinitionId(User user) => user.Role switch
            {
                UserRoles.Project.Owner => AzureRoleDefinition.Contributor,
                UserRoles.Project.Member => AzureRoleDefinition.Reader,
                _ => throw new NotSupportedException($"User '{user.Id}' has an unsupported role '{user.Role}'")
            };
        }
    }
}
