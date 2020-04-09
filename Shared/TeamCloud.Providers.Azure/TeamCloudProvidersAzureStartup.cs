/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using TeamCloud.Providers.Azure;

[assembly: FunctionsStartup(typeof(TeamCloudProvidersAzureStartup))]

namespace TeamCloud.Providers.Azure
{
    public sealed class TeamCloudProvidersAzureStartup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));
        }
    }
}
