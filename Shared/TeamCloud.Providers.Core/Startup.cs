/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamCloud.Providers.Core;
using TeamCloud.Providers.Core.Configuration;

[assembly: FunctionsStartup(typeof(TeamCloudCoreStartup))]

namespace TeamCloud.Providers.Core
{
    public class TeamCloudCoreStartup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson();

            builder.Services
                .TryAddSingleton<IOrchestrationConfiguration>(new OrchestrationConfiguration());
        }
    }
}
