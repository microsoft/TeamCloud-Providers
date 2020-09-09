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
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamCloud.Azure;
using TeamCloud.Azure.Resources;
using TeamCloud.Configuration;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps;
using TeamCloud.Providers.Azure.DevOps.Orchestrations;
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
                .AddSingleton(GetConfiguration(builder.Services))
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

        private static IConfiguration GetConfiguration(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();

            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            return ConfigureEnvironment(environment, configuration).Build();
        }

        private static IConfigurationBuilder ConfigureEnvironment(IHostEnvironment hostingEnvironment, IConfiguration configuration)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddConfiguration(configuration);

            configuration = configurationBuilder
                .AddConfigurationService()
                .Build(); // refresh configuration root to get configuration service settings

            var keyVaultName = configuration["KeyVaultName"];

            if (!string.IsNullOrEmpty(keyVaultName))
            {
                // we use the managed identity of the service to authenticate at the KeyVault
                var azureServiceTokenProvider = new AzureServiceTokenProvider();

                using var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                configurationBuilder.AddAzureKeyVault($"https://{keyVaultName}.vault.azure.net/", keyVaultClient, new DefaultKeyVaultSecretManager());
            }
            else if (hostingEnvironment.IsDevelopment())
            {
                // for development we use the local secret store as a fallback if not KeyVaultName is provided
                // see: https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1

                try
                {
                    configurationBuilder.AddUserSecrets<Startup>();
                }
                catch (InvalidOperationException exc) when (exc.Message.Contains(nameof(UserSecretsIdAttribute), StringComparison.Ordinal))
                {
                    // swallow this exception and resume without user secrets
                }
            }

            return configurationBuilder;
        }

    }
}
