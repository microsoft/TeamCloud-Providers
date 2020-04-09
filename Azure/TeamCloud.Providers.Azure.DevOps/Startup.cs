/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.DevOps;
using TeamCloud.Providers.Azure.DevOps.Orchestrations;
using TeamCloud.Providers.Core;

[assembly: FunctionsStartup(typeof(Startup))]

// FunctionsImport will enable the compiler to early bind
// the assembly of the referenced type. this is required
// to enable the FunctionsInDependencies (see csproj)
// feature of the Azure Functions SDK.

[assembly: FunctionsImport(typeof(TeamCloudProvidersCoreStartup))]

namespace TeamCloud.Providers.Azure.DevOps
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
                .AddTeamCloudCommandOrchestration(configuration =>
                {
                    configuration
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(ProjectCreateOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(ProjectUpdateOrchestration))
                        .MapCommand<ProviderProjectDeleteCommand>(nameof(ProjectDeleteOrchestration))
                        .IgnoreCommand<IProviderCommand>();
                });
        }
    }
}
