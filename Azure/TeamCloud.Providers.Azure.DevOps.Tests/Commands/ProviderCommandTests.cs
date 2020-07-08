/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Azure.DevOps.Commands
{
    public abstract class ProviderCommandTests
    {
        public static TimeSpan DefaultCallbackResultTimeout { get; set; } = TimeSpan.FromMinutes(5);

        private static Task<string> AcquireTokenAsync()
        {
            // ensure we disable SSL verfication for this process when using the Azure CLI to aqcuire MSI token.
            // otherwise our code will fail in dev scenarios where a dev proxy like fiddler is running to sniff
            // http traffix between our services or between service and other reset apis (e.g. Azure)

            Environment.SetEnvironmentVariable("AZURE_CLI_DISABLE_CONNECTION_VERIFICATION", "1", EnvironmentVariableTarget.Process);

            return new AzureServiceTokenProvider("RunAs=Developer;DeveloperTool=AzureCLI")
                .GetAccessTokenAsync(AzureEnvironment.AzureGlobalCloud.ResourceManagerEndpoint);
        }

        protected static bool WaitFor(Func<bool> predicate, int timeout)
        {
            var absoluteTimeout = DateTime.UtcNow.AddMilliseconds(timeout);

            while (!predicate())
            {
                if (DateTime.UtcNow < absoluteTimeout)
                    Thread.Sleep(Math.Min(timeout / 10, 10 * 1000));
                else
                    return false;
            }

            return true;
        }

        protected ProviderCommandTests(ProviderService providerService, ILogger logger = null)
        {
            ProviderService = providerService ?? throw new ArgumentNullException(nameof(providerService));
            Logger = logger ?? NullLogger.Instance;
        }


        protected ProviderService ProviderService { get; }

        protected ILogger Logger { get; }

        protected async Task<User> GetUserAsync()
        {
            var token = await AcquireTokenAsync()
                .ConfigureAwait(false);

            var jwtToken = new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

            return (jwtToken.Payload.TryGetValue("oid", out var oidValue) && Guid.TryParse(oidValue.ToString(), out Guid oid))
                ? new User() { Id = oid.ToString() }
                : null;
        }

        protected async Task<TCommand> CreateCommandAsync<TCommand>(string name = default, Action<JObject> modifyCommandJson = default)
            where TCommand : class, IProviderCommand
        {
            var resourceName = $"{this.GetType().FullName}.json";

            if (!string.IsNullOrWhiteSpace(name))
                resourceName = Path.ChangeExtension(resourceName, $".{name.Trim()}.json");

            resourceName = this.GetType().Assembly.GetManifestResourceNames()
                .SingleOrDefault(n => n.Equals(resourceName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(resourceName))
                return default;

            using var stream = this.GetType().Assembly.GetManifestResourceStream(resourceName);
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);

            var commandJson = JObject.Load(jsonReader);
            var commandUser = await GetUserAsync().ConfigureAwait(false);

            (commandJson.SelectToken("$.commandId") as JValue)?.SetValue(Guid.NewGuid());

            var user = commandJson.SelectToken("$.user");

            if (user is null)
                commandJson.Add("user", JObject.FromObject(commandUser));
            else
                user.Replace(JObject.FromObject(commandUser));

            // use command modification callback if provided
            modifyCommandJson?.Invoke(commandJson);

            return commandJson.ToObject<TCommand>();
        }



        protected async Task<ICommandResult> SendCommandAsync(IProviderCommand providerCommand, bool waitForCallbackResult = false, TimeSpan waitForCallbackResultTimeout = default)
        {
            if (providerCommand is null)
                throw new ArgumentNullException(nameof(providerCommand));

            if (!(await ProviderService.StartAsync().ConfigureAwait(false)))
                throw new InvalidOperationException("Provider host not running");

            var message = new ProviderCommandMessage(
                providerCommand,
                CallbackHost.Instance.GetCallbackUrl(providerCommand));

            Logger.LogInformation($"{nameof(SendCommandAsync)}: {JsonConvert.SerializeObject(providerCommand)}");

            var response = await ProviderService.BaseUrl
                .AppendPathSegment("api/command")
                .PostJsonAsync(message)
                .ConfigureAwait(false);

            var commandResult = await response.Content
                .ReadAsAsync<ICommandResult>()
                .ConfigureAwait(false);

            if (waitForCallbackResult)
            {
                commandResult = await GetCommandResultAsync(providerCommand.CommandId, waitForCallbackResult, waitForCallbackResultTimeout)
                    .ConfigureAwait(false);
            }

            return commandResult;
        }

        protected async Task<ICommandResult> GetCommandResultAsync(Guid commandId, bool waitForCallbackResult = false, TimeSpan waitForCallbackResultTimeout = default)
        {
            if (waitForCallbackResult)
            {
                var timeout = waitForCallbackResultTimeout == default
                    ? (int)DefaultCallbackResultTimeout.Duration().TotalMilliseconds
                    : (int)waitForCallbackResultTimeout.Duration().TotalMilliseconds;

                if (!WaitFor(() => !(CallbackHost.Instance.GetCommandResult(commandId) is null), timeout))
                    throw new TimeoutException($"Waiting for callback result failed after {TimeSpan.FromMilliseconds(timeout)}");
            }

            var commandResult = CallbackHost.Instance
                .GetCommandResult(commandId);

            commandResult ??= await ProviderService.BaseUrl
                .AppendPathSegment($"api/command/{commandId}")
                .GetJsonAsync<ICommandResult>()
                .ConfigureAwait(false);

            Logger.LogInformation($"{nameof(GetCommandResultAsync)}: {JsonConvert.SerializeObject(commandResult)}");

            return commandResult;
        }

    }
}
