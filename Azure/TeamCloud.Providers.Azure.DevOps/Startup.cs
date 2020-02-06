/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers;
using Microsoft.Extensions.DependencyInjection;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.DevOps.Orchestrations;
using LocalStartup = TeamCloud.Providers.Azure.DevOps.Startup;
using ImportStartup = Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Startup;

[assembly: FunctionsStartup(typeof(LocalStartup))]

// FunctionsImport will enable the compiler to early bind
// the assembly of the referenced type. this is required
// to enable the FunctionsInDependencies (see csproj)
// feature of the Azure Functions SDK.

[assembly: FunctionsImport(typeof(ImportStartup))]

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
                .AddCommandOrchestration(config =>
                {
                    config.Map<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration));
                    config.Map<ProjectCreateCommand>(nameof(ProjectCreateOrchestration));
                    config.Map<ProjectUpdateCommand>(nameof(ProjectUpdateOrchestration));
                    config.Map<ProjectDeleteCommand>(nameof(ProjectDeleteOrchestration));
                });
        }
    }
}
