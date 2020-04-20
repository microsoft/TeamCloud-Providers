using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Model;
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

                    var roleAssignments = project.Users
                        .ToRoleAssignments(role => role.Equals(UserRoles.Project.Owner, StringComparison.OrdinalIgnoreCase)
                            ? AzureRoleDefinition.Contributor
                            : AzureRoleDefinition.Reader);

                    await resource
                        .SetRoleAssignmentsAsync(roleAssignments)
                        .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProjectUsersActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }

    }
}
