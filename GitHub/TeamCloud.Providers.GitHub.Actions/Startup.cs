/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamCloud.Azure;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Core;
using TeamCloud.Providers.Core.Configuration;
using TeamCloud.Providers.GitHub.Actions;
using TeamCloud.Providers.GitHub.Actions.Orchestrations;
using TeamCloud.Providers.GitHub.Actions.Services;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: FunctionsImport(typeof(TeamCloudProvidersCoreStartup))]
[assembly: FunctionsImport(typeof(TeamCloudOrchestrationStartup))]

namespace TeamCloud.Providers.GitHub.Actions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

#pragma warning disable CS0618 // Type or member is obsolete

            var hostingEnvironment = builder.Services
                .BuildServiceProvider()
                .GetService<IHostingEnvironment>();

#pragma warning restore CS0618 // Type or member is obsolete

            builder.Services
                // .AddSingleton(GetConfiguration(builder.Services))
                .AddLogging()
                .AddMvcCore()
                .AddNewtonsoftJson();

            builder.Services
                .AddTeamCloudHttp()
                .AddTeamCloudAzure(configuration => { })
                .AddTeamCloudCommandOrchestration(configuration =>
                {
                    configuration
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration), (command) => TimeSpan.FromMinutes(5))
                        // .MapCommand<ProviderRegisterCommand>(nameof(CommandDispatchOrchestration), (command) => TimeSpan.FromMinutes(5))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(CommandDispatchOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(CommandDispatchOrchestration))
                        .MapCommand<ProviderProjectDeleteCommand>(nameof(CommandDispatchOrchestration))
                        .MapCommand<ProviderTeamCloudUserCreateCommand>(nameof(CommandDispatchOrchestration))
                        .MapCommand<ProviderTeamCloudUserDeleteCommand>(nameof(CommandDispatchOrchestration))
                        .MapCommand<ProviderTeamCloudUserUpdateCommand>(nameof(CommandDispatchOrchestration))
                        .IgnoreCommand<IProviderCommand>();
                });

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

            // builder.Services
            //     .AddSingleton<IAuthenticationService, AuthenticationService>();

            builder.Services
                .AddSingleton<GitHubService>();
        }
    }
}
