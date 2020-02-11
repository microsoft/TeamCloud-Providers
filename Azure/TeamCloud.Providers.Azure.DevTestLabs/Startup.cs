/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using TeamCloud.Azure;
using TeamCloud.Azure.Deployment;
using TeamCloud.Azure.Deployment.Providers;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.DevTestLabs;
using TeamCloud.Providers.Azure.DevTestLabs.Orchestrations;
using TeamCloud.Providers.Core;

[assembly: FunctionsStartup(typeof(Startup))]

// FunctionsImport will enable the compiler to early bind
// the assembly of the referenced type. this is required
// to enable the FunctionsInDependencies (see csproj)
// feature of the Azure Functions SDK.

[assembly: FunctionsImport(typeof(TeamCloudCoreStartup))]

namespace TeamCloud.Providers.Azure.DevTestLabs
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson();

            builder.Services
                .AddTeamCloudHttp()
                .AddTeamCloudAzure(configuration =>
                {
                    configuration
                        .AddDeployment()
                        .SetDeploymentArtifactsProvider<AzureStorageArtifactsProvider>();
                })
                .AddTeamCloudCommandOrchestration(configuration =>
                {
                    configuration
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(ProjectCreateOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(ProjectUpdateOrchestration))
                        .MapCommand<ProviderProjectDeleteCommand>(nameof(ProjectDeleteOrchestration))
                        .IgnoreCommand<ICommand>();
                });
        }
    }
}
