/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.AppInsights;
using TeamCloud.Providers.Azure.AppInsights.Orchestrations;
using TeamCloud.Providers.Core;

[assembly: FunctionsStartup(typeof(Startup))]

// FunctionsImport will enable the compiler to early bind
// the assembly of the referenced type. this is required
// to enable the FunctionsInDependencies (see csproj)
// feature of the Azure Functions SDK.

[assembly: FunctionsImport(typeof(TeamCloudCoreStartup))]

namespace TeamCloud.Providers.Azure.AppInsights
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
                .AddCommandOrchestration(config =>
                {
                    config.MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration));
                    config.MapCommand<ProviderProjectCreateCommand>(nameof(ProjectCreateOrchestration));
                    config.MapCommand<ProviderProjectUpdateCommand>(nameof(ProjectUpdateOrchestration));
                    config.MapCommand<ProviderProjectDeleteCommand>(nameof(ProjectDeleteOrchestration));
                    config.IgnoreCommand<ICommand>();
                });
        }
    }
}
