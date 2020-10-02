/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Reflection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamCloud.Azure;
using TeamCloud.Azure.Resources;
using TeamCloud.Configuration;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps;
using TeamCloud.Providers.Azure.DevOps.Orchestrations.Commands;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Providers.Core;
using TeamCloud.Providers.Core.Configuration;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: FunctionsImport(typeof(TeamCloudProvidersCoreStartup))]
[assembly: FunctionsImport(typeof(TeamCloudOrchestrationStartup))]

namespace TeamCloud.Providers.Azure.DevOps
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services
                .AddLogging()
                .AddMvcCore()
                .AddNewtonsoftJson();

            builder.Services
                .AddTeamCloudOptions(Assembly.GetExecutingAssembly())
                .AddTeamCloudHttp()
                .AddTeamCloudAzure(configuration =>
                {
                    configuration
                        .AddResources();
                })
                .AddTeamCloudCommandOrchestration(configuration =>
                {
                    configuration
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterCommandOrchestration), settings => settings.OrchstrationTimeout = (command) => TimeSpan.FromMinutes(5))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(ProviderProjectCreateCommandOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(ProviderProjectUpdateCommandOrchestration))
                        .MapCommand<ProviderProjectDeleteCommand>(nameof(ProviderProjectDeleteCommandOrchestration))
                        .IgnoreCommand<IProviderCommand>();
                });

            var serviceProvider = builder.Services
                .BuildServiceProvider();

            var hostEnvironment = serviceProvider
                .GetRequiredService<IHostEnvironment>();

            var configuration = serviceProvider
                .GetRequiredService<IConfiguration>();

            if (hostEnvironment.IsDevelopment())
            {
                builder.Services
                    .AddSingleton<ISecretsService, StorageSecretsService>();
            }
            else
            {
                // we use the managed identity of the service to authenticate at the KeyVault
                builder.Services
                    .AddSingleton<IKeyVaultClient>(provider => new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)));

                builder.Services
                    .AddSingleton<ISecretsService, VaultSecretsServices>();
            }

            if (string.IsNullOrEmpty(configuration.GetValue<string>("Cache:Configuration")))
            {
                builder.Services
                    .AddDistributedMemoryCache();
            }
            else
            {
                builder.Services
                    .AddDistributedRedisCache(options => configuration.Bind("Cache", options));
            }

            builder.Services
                .AddDistributedMemoryCache()
                .AddSingleton<IAuthenticationService, AuthenticationService>();
        }
    }
}
