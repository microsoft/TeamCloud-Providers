/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Flurl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Azure.DevOps
{
    public sealed class CallbackHost
    {
        private static readonly Lazy<CallbackHost> InstanceFactory = new Lazy<CallbackHost>(() => new CallbackHost());

        public static CallbackHost Instance => InstanceFactory.Value;

        private readonly ConcurrentDictionary<Guid, ICommandResult> commandResults = new ConcurrentDictionary<Guid, ICommandResult>();

        private readonly IWebHost host;

        private CallbackHost()
        {
            host = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.Configure<KestrelServerOptions>(options =>
                    {
                        options.AllowSynchronousIO = true;
                    });
                })
                .Configure(app => app.Run(async context =>
                {
                    try
                    {
                        var match = Regex.Match(context.Request.Path, @"\/?callback\/((\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1})");

                        if (match.Success && context.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
                        {
                            var requestMessage = context
                                .GetHttpRequestMessage();

                            var commandResult = await requestMessage.Content
                                .ReadAsAsync<ICommandResult>()
                                .ConfigureAwait(false);

                            commandResults[commandResult.CommandId] = commandResult;

                            Debug.WriteLine($"Received result for command '{commandResult.CommandId}':{Environment.NewLine}{JsonConvert.SerializeObject(commandResult, Formatting.Indented)}");
                        }

                        context.Response.StatusCode = (int)(match.Success
                            ? HttpStatusCode.OK
                            : HttpStatusCode.NotFound);
                    }
                    catch
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                    finally
                    {
                        await context.Response
                            .StartAsync()
                            .ConfigureAwait(false);
                    }
                }))
                .Build();

            host.Start();
        }

        public string GetBaseUrl()
            => host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase));

        public string GetCallbackUrl(IProviderCommand providerCommand)
            => GetBaseUrl().AppendPathSegment($"callback/{providerCommand.CommandId}");

        public ICommandResult GetCommandResult(Guid commandId)
        {
            if (commandResults.TryGetValue(commandId, out var commandResult))
                return commandResult;

            Debug.WriteLine($"Waiting for command result '{commandId}' ...");

            return null;
        }
    }
}
