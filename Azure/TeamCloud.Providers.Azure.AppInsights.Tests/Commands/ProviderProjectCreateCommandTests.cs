/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data.Core;
using TeamCloud.Providers.Azure.AppInsights.Conditional;
using TeamCloud.Providers.Testing;
using TeamCloud.Providers.Testing.Services;
using Xunit;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Azure.AppInsights.Commands
{
    [Collection(ProviderContext.Name)]
    public class ProviderProjectCreateCommandTests : ProviderAzureTests
    {
        public ProviderProjectCreateCommandTests(ProviderService providerService, ITestOutputHelper outputHelper)
            : base(providerService, outputHelper)
        { }

        [ConditionalFact(ConditionalFactPlatforms.Windows)]
        public override async Task ExecuteAsync()
        {
            await RegisterAsync()
                .ConfigureAwait(false);

            var command = await CreateCommandAsync<ProviderProjectCreateCommand>(modifyCommandJson: ModifyCommandPayload)
                .ConfigureAwait(false);

            var commandResult = await SendCommandAsync(command)
                .ConfigureAwait(false);

            Assert.Equal(CommandRuntimeStatus.Running, commandResult.RuntimeStatus);

            commandResult = await GetCommandResultAsync(command.CommandId, true, commandResult.Timeout)
                .ConfigureAwait(false);

            Assert.Equal(CommandRuntimeStatus.Completed, commandResult.RuntimeStatus);

            await AssertResourceGroupAsync(command)
                .ConfigureAwait(false);

            void ModifyCommandPayload(JObject commandJson)
            {
                var testId = base.Test.TestCase.UniqueID.ToGuid();

                (commandJson.SelectToken("$.payload.id") as JValue)?.SetValue(testId);
                (commandJson.SelectToken("$.payload.name") as JValue)?.SetValue($"Project_{testId}");
            }
        }

        private async Task AssertResourceGroupAsync(ProviderProjectCreateCommand command)
        {
            var resourceGroup = await AzureResourceService
                .GetResourceGroupAsync(ResourceGroupSubscriptionId.Value, ResourceGroupName, true)
                .ConfigureAwait(false);

            var resources = await resourceGroup.GetResourcesAsync()
                .ToListAsync()
                .ConfigureAwait(false);

            var resource = resources
                .SingleOrDefault(resource => resource.ResourceId.ResourceTypeFullName.Equals("microsoft.insights/components", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(resource);

            foreach (var user in command.Payload.Users)
            {
                switch (user.ProjectMembership(command.Payload.Id.ToString()).Role)
                {
                    case ProjectUserRole.Owner:

                        await resource
                            .ShouldHaveRoleAssignmentAsync(user, AzureRoleDefinition.Contributor)
                            .ConfigureAwait(false);

                        break;

                    case ProjectUserRole.Member:

                        await resource
                            .ShouldHaveRoleAssignmentAsync(user, AzureRoleDefinition.Reader)
                            .ConfigureAwait(false);

                        break;

                    case ProjectUserRole.None:

                        await resource
                            .ShouldHaveNoRoleAssignmentAsync(user)
                            .ConfigureAwait(false);

                        break;
                };
            }
        }
    }
}
