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
                    var resource = await azureResourceService
                        .GetResourceAsync(resourceId, throwIfNotExists: true)
                        .ConfigureAwait(false);

                    var roleAssignments = (project.Users ?? Enumerable.Empty<User>())
                        .ToDictionary(user => user.Id, user => Enumerable.Repeat((user.ProjectMembership(project.Id)?.Role ?? ProjectUserRole.None).ToRoleDefinitionId(), 1).Where(rdid => rdid != Guid.Empty))
                        .Where(kvp => kvp.Value.Any())
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    var identities = await resource
                        .GetIdentitiesAsync()
                        .ToListAsync()
                        .ConfigureAwait(false);

                    foreach (var identity in identities)
                    {
                        var roleDefinitionIds = new HashSet<Guid>()
                        {
                            AzureRoleDefinition.Reader
                        };

                        if (roleAssignments.TryGetValue(identity.PrincipalId, out var ids))
                        {
                            ids.ToList().ForEach(id => _ = roleDefinitionIds.Add(id));
                        }

                        roleAssignments[identity.PrincipalId] = roleDefinitionIds.AsEnumerable();
                    }

                    if (roleAssignments.Any())
                    {
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
        }
    }
}
