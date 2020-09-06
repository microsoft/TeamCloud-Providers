﻿/**
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
using TeamCloud.Audit;
using TeamCloud.Azure;
using TeamCloud.Azure.Deployment;
using TeamCloud.Azure.Deployment.Providers;
using TeamCloud.Azure.Resources;
using TeamCloud.Configuration;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Orchestration.Deployment;
using TeamCloud.Providers.Azure.DevTestLabs;
using TeamCloud.Providers.Azure.DevTestLabs.Orchestrations;
using TeamCloud.Providers.Core;
using TeamCloud.Providers.Core.Configuration;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: FunctionsImport(typeof(TeamCloudProvidersCoreStartup))]
[assembly: FunctionsImport(typeof(TeamCloudOrchestrationStartup))]
[assembly: FunctionsImport(typeof(TeamCloudOrchestrationDeploymentStartup))]

namespace TeamCloud.Providers.Azure.DevTestLabs
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
                        .AddResources()
                        .AddDeployment()
                        .SetDeploymentArtifactsProvider<AzureStorageArtifactsProvider>();
                })
                .AddTeamCloudCommandOrchestration(configuration =>
                {
                    configuration
                        .MapCommand<ProviderEventCommand>(nameof(ProviderEventOrchestration))
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration), settings => settings.OrchstrationTimeout = (command) => TimeSpan.FromMinutes(5))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(ProjectCreateOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(ProjectUpdateOrchestration))
                        .IgnoreCommand<IProviderCommand>()
                        .SubscribeEvent(ProviderEventSubscription.ResourceWriteSuccess)
                        .SubscribeEvent(ProviderEventSubscription.ResourceDeleteSuccess);
                });
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
