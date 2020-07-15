/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using TeamCloud.Azure;
using TeamCloud.Azure.Deployment;
using TeamCloud.Azure.Deployment.Providers;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Model.Data.Core;
using TeamCloud.Providers.Azure.AppInsights.Options;
using TeamCloud.Providers.Testing;
using TeamCloud.Providers.Testing.Services;
using Xunit;
using Xunit.Abstractions;
using AzureResourceGroup = TeamCloud.Model.Data.Core.AzureResourceGroup;

namespace TeamCloud.Providers.Azure.AppInsights.Commands
{
    public abstract class ProviderAzureTests : ProviderCoreTests
    {
        protected static readonly IAzureSessionService AzureSessionService = new AzureSessionService();

        protected static readonly string AzureResourceLocation = "EastUs";

        protected static readonly IAzureResourceService AzureResourceService = new AzureResourceService(AzureSessionService);

        protected static readonly IAzureDeploymentOptions AzureDeploymentOptions = new AzureDeploymentOptions();

        protected static readonly IAzureDeploymentService AzureDeploymentService = new AzureDeploymentService(AzureDeploymentOptions, AzureSessionService, NoneStorageArtifactsProvider.Instance);

        protected ProviderAzureTests(ProviderService providerService, ITestOutputHelper outputHelper)
            : base(providerService, outputHelper)
        { }

        protected async Task RegisterAsync()
        {
            var user = await GetUserAsync()
                .ConfigureAwait(false);

            var command = new ProviderRegisterCommand(user, new ProviderConfiguration()
            {
                TeamCloudApplicationInsightsKey = Guid.Empty.ToString()
            });

            var commandResult = await SendCommandAsync(command, true)
                .ConfigureAwait(false);

            Assert.Equal(command.CommandId, commandResult.CommandId);
            Assert.Equal(CommandRuntimeStatus.Completed, commandResult.RuntimeStatus);
        }

        protected Guid? ResourceGroupSubscriptionId
            => Guid.TryParse(Configuration.GetValue<string>("subscriptionId"), out var subscriptionId) ? subscriptionId : default;

        protected string ResourceGroupName
            => $"TEST-{Test.TestCase.UniqueID}-{ProviderService.Started.GetValueOrDefault().Ticks}";

        protected string ResourceGroupLocation
            => AzureResourceLocation;

        protected override async Task<TCommand> CreateCommandAsync<TCommand>(string name = null, Action<JObject> modifyCommandJson = null)
        {
            var command = await base
                .CreateCommandAsync<TCommand>(name, modifyCommandJson)
                .ConfigureAwait(false);

            if (command.Payload is Project project)
            {
                project.ResourceGroup ??= await GetResourceGroupAsync()
                    .ConfigureAwait(false);

                var user = await GetUserAsync()
                    .ConfigureAwait(false);

                if (!project.Users.Contains(user))
                {
                    user.EnsureProjectMembership(project.Id, ProjectUserRole.Owner);

                    project.Users.Add(user);
                }
            }

            return command;
        }

        protected async Task<AzureResourceGroup> GetResourceGroupAsync()
        {
            if (ResourceGroupSubscriptionId.HasValue)
            {
                var resourceGroup = await AzureResourceService
                    .GetResourceGroupAsync(ResourceGroupSubscriptionId.Value, ResourceGroupName)
                    .ConfigureAwait(false);

                if (resourceGroup is null)
                {
                    var subscription = await AzureResourceService
                        .GetSubscriptionAsync(ResourceGroupSubscriptionId.Value, true)
                        .ConfigureAwait(false);

                    resourceGroup = await subscription
                        .CreateResourceGroupAsync(ResourceGroupName, AzureResourceLocation)
                        .ConfigureAwait(false);
                }

                await resourceGroup
                    .SetTagAsync("Test", Test.DisplayName)
                    .ConfigureAwait(false);

                if (ProviderServicePrincipalId.HasValue)
                {
                    var roleAssignments = await resourceGroup
                        .GetRoleAssignmentsAsync(ProviderServicePrincipalId.ToString())
                        .ConfigureAwait(false);

                    if (!roleAssignments.Contains(AzureRoleDefinition.Contributor))
                    {
                        await resourceGroup
                            .AddRoleAssignmentAsync(ProviderServicePrincipalId.ToString(), AzureRoleDefinition.Contributor)
                            .ConfigureAwait(false);
                    }
                }

                return new AzureResourceGroup()
                {
                    Id = resourceGroup.ResourceId.ToString(),
                    Name = ResourceGroupName,
                    Region = ResourceGroupLocation,
                    SubscriptionId = ResourceGroupSubscriptionId.GetValueOrDefault()
                };
            }
            else
            {
                throw new NotSupportedException("Missing configuration value 'subscriptionId'");
            }
        }

        public override async Task DisposeAsync()
        {
            if (ResourceGroupSubscriptionId.HasValue)
            {
                var resourceGroup = await AzureResourceService
                    .GetResourceGroupAsync(ResourceGroupSubscriptionId.Value, ResourceGroupName)
                    .ConfigureAwait(false);

                if (resourceGroup != null)
                {
                    await resourceGroup
                        .DeleteAsync(true)
                        .ConfigureAwait(false);
                }
            }

            await base
                .DisposeAsync()
                .ConfigureAwait(false);
        }
    }
}
