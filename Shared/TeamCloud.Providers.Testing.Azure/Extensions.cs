/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;
using Xunit;

namespace TeamCloud.Providers.Testing
{
    public static class Extensions
    {
        public static async Task ShouldHaveRoleAssignmentAsync(this AzureResource azureResource, User user, Guid roleDefinition)
        {
            if (azureResource is null)
                throw new ArgumentNullException(nameof(azureResource));

            if (user is null)
                throw new ArgumentNullException(nameof(user));

            var roleAssignments = await azureResource.GetRoleAssignmentsAsync(user.Id).ConfigureAwait(false);

            Assert.Contains(roleAssignments, roleAssignment => roleAssignment == roleDefinition);
        }

        public static async Task ShouldHaveNoRoleAssignmentAsync(this AzureResource azureResource, User user)
        {
            if (azureResource is null)
                throw new ArgumentNullException(nameof(azureResource));

            if (user is null)
                throw new ArgumentNullException(nameof(user));

            var roleAssignments = await azureResource.GetRoleAssignmentsAsync(user.Id).ConfigureAwait(false);

            Assert.Empty(roleAssignments);
        }
    }
}
