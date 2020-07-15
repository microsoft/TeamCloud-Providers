/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Testing.Services
{
    public sealed class OrchestratorService
    {
        private static readonly Lazy<OrchestratorService> InstanceFactory
            = new Lazy<OrchestratorService>(() => new OrchestratorService());

        public static OrchestratorService Instance
            => InstanceFactory.Value;

        private readonly ConcurrentDictionary<Guid, ICommandResult> results
            = new ConcurrentDictionary<Guid, ICommandResult>();

        private readonly IWebHost host;

        private OrchestratorService()
        {
            host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<OrchestratorStartup>()
                .ConfigureServices(services =>
                {
                    services.Configure<KestrelServerOptions>(options =>
                    {
                        options.AllowSynchronousIO = true;
                        options.ListenLocalhost(7071);
                    });
                })
                .Build();
        }

        internal Task StartAsync()
            => host.StartAsync();

        public string BaseUrl
            => host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses
            .FirstOrDefault(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))?
            .AppendPathSegment("/api");

        public string GetCallbackUrl(IProviderCommand providerCommand)
            => BaseUrl.AppendPathSegment($"callback/{providerCommand.CommandId}");

        public ICommandResult GetCommandResult(Guid commandId)
            => results.TryGetValue(commandId, out var commandResult) ? commandResult : null;

        public bool AddCommandResult(ICommandResult commandResult)
            => results.TryAdd(commandResult.CommandId, commandResult);
    }
}
