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
using TeamCloud.Orchestration.Auditing;
using TeamCloud.Providers.Azure.DevOps;
using TeamCloud.Providers.Azure.DevOps.Orchestrations;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Providers.Core;
using TeamCloud.Providers.Core.Configuration;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: FunctionsImport(typeof(TeamCloudProvidersCoreStartup))]
[assembly: FunctionsImport(typeof(TeamCloudOrchestrationStartup))]
[assembly: FunctionsImport(typeof(TeamCloudOrchestrationAuditingStartup))]

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
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration), settings => settings.OrchstrationTimeout = (command) => TimeSpan.FromMinutes(5))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(ProjectCreateOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(ProjectUpdateOrchestration))
                        .MapCommand<ProviderEventCommand>(nameof(ProviderEventOrchestration))
                        .IgnoreCommand<IProviderCommand>();
                });

            var serviceProvider = builder.Services
                .BuildServiceProvider();

            var hostingEnvironment = serviceProvider
                .GetRequiredService<IHostingEnvironment>();

            var configuration = serviceProvider
                .GetRequiredService<IConfiguration>();

            if (hostingEnvironment.IsDevelopment())
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
