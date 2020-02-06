/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers;
using Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Results;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services
                .TryAddSingleton<IConfiguration>(new Configuration());

            builder.Services
                .TryAddSingleton<IProviderCommandResultMessageQueueClient, ProviderCommandResultMessageQueueClient>();
        }
    }
}
