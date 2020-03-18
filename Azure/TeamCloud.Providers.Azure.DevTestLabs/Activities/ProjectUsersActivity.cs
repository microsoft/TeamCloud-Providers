using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Core;

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
            [ActivityTrigger] (Project project, string resourceId) input,
            ILogger log)
        {
            var roleAssignments = input.project.Users
                .ToRoleAssignments(role => role.Equals(UserRoles.Project.Owner, StringComparison.OrdinalIgnoreCase)
                    ? AzureRoleDefinition.Contributor
                    : AzureRoleDefinition.Reader);

            var resource = await azureResourceService
                .GetResourceAsync(input.resourceId)
                .ConfigureAwait(false);
            
            await resource
                .SetRoleAssignmentsAsync(roleAssignments)
                .ConfigureAwait(false);
        }


    }
}
