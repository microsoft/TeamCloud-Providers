using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Core;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public class ProjectUsersActivity
    {
        private readonly IAzureResourceService azureResourceService;

        public ProjectUsersActivity(IAzureResourceService azureResourceService)
        {
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        [FunctionName(nameof(ProjectUsersActivity))]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            try
            {
                var (project, resourceId) = functionContext.GetInput<(Project, string)>();

                var resource = await azureResourceService
                    .GetResourceAsync(resourceId, throwIfNotExists: true)
                    .ConfigureAwait(false);

                var roleAssignments = project.Users
                    .ToRoleAssignments(role => role.Equals(UserRoles.Project.Owner, StringComparison.OrdinalIgnoreCase)
                        ? AzureRoleDefinition.Contributor
                        : AzureRoleDefinition.Reader);

                await resource
                    .SetRoleAssignmentsAsync(roleAssignments)
                    .ConfigureAwait(false);
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableException))
            {
                throw serializableException;
            }
        }


    }
}
