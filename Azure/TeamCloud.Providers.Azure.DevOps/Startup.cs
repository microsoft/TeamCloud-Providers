/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Orchestration;
using TeamCloud.Orchestration.Auditing;
using TeamCloud.Providers.Azure.DevOps;
using TeamCloud.Providers.Azure.DevOps.Orchestrations;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Providers.Core;

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

#pragma warning disable CS0618 // Type or member is obsolete

            var hostingEnvironment = builder.Services
                .BuildServiceProvider()
                .GetService<IHostingEnvironment>();

#pragma warning restore CS0618 // Type or member is obsolete

            builder.Services
                .AddMvcCore()
                .AddNewtonsoftJson();

            builder.Services
                .AddTeamCloudHttp()
                .AddTeamCloudCommandOrchestration(configuration =>
                {
                    configuration
                        .MapCommand<ProviderRegisterCommand>(nameof(ProviderRegisterOrchestration), (command) => TimeSpan.FromMinutes(5))
                        .MapCommand<ProviderProjectCreateCommand>(nameof(ProjectCreateOrchestration))
                        .MapCommand<ProviderProjectUpdateCommand>(nameof(ProjectUpdateOrchestration))
                        .MapCommand<ProviderProjectDeleteCommand>(nameof(ProjectDeleteOrchestration))
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

            builder.Services
                .AddSingleton<IAuthenticationService, AuthenticationService>();
        }
    }
}
