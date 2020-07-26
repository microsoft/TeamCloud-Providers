/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamCloud.Azure;
using TeamCloud.Azure.Directory;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Model.Data.Core;
using TeamCloud.Model.Validation;
using TeamCloud.Providers.Testing.Diagnostics;
using TeamCloud.Providers.Testing.Services;
using Xunit;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Testing.Commands
{

    public abstract class ProviderCommandCoreTests : IAsyncLifetime
    {
        protected static readonly IAzureSessionService AzureSessionService
            = new AzureSessionService();

        protected static readonly IAzureDirectoryService AzureDirectoryService
            = new AzureDirectoryService(AzureSessionService);

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

        private static bool WaitFor(Func<bool> predicate, int timeout)
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

        private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeout)
        {
            var absoluteTimeout = DateTime.UtcNow.AddMilliseconds(timeout);

            while (!await predicate().ConfigureAwait(false))
            {
                if (DateTime.UtcNow < absoluteTimeout)
                    Thread.Sleep(Math.Min(timeout / 10, 10 * 1000));
                else
                    return false;
            }

            return true;
        }

        private readonly Lazy<IConfiguration> configuration;

        protected ProviderCommandCoreTests(ProviderService providerService, ITestOutputHelper outputHelper)
        {
            ProviderService = providerService ?? throw new ArgumentNullException(nameof(providerService));
            Logger = XUnitLogger.Create(GetType(), outputHelper);
            Test = outputHelper.GetTest();

            configuration = new Lazy<IConfiguration>(() => BuildConfiguration(new ConfigurationBuilder()));
        }

        protected ProviderService ProviderService { get; }

        protected Guid? ProviderServicePrincipalId { get; private set; }

        protected ILogger Logger { get; }

        protected ITest Test { get; }

        protected IConfiguration Configuration
            => configuration.Value;

        protected virtual IConfiguration BuildConfiguration(IConfigurationBuilder configurationBuilder)
            => (configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder)))
            .AddUserSecrets(GetType().Assembly)
            .Build();

        protected async Task RegisterAsync()
        {
            var user = await GetUserAsync()
                .ConfigureAwait(false);

            var command = new ProviderRegisterCommand(user, new ProviderConfiguration()
            {
                TeamCloudApplicationInsightsKey = Guid.Empty.ToString()
            });

            var commandResult = await SendCommandAsync(command, true)
                .ConfigureAwait(false);

            Assert.Equal(command.CommandId, commandResult.CommandId);
            Assert.Equal(CommandRuntimeStatus.Completed, commandResult.RuntimeStatus);
        }

        protected async Task<User> GetUserAsync(string identifier = null)
        {
            Guid? userId = default;

            if (string.IsNullOrEmpty(identifier))
            {
                var token = await AcquireTokenAsync()
                    .ConfigureAwait(false);

                var jwtToken = new JwtSecurityTokenHandler()
                    .ReadJwtToken(token);

                if (jwtToken.Payload.TryGetValue("oid", out var oidValue) && Guid.TryParse(oidValue.ToString(), out Guid oid))
                    userId = oid;
            }
            else
            {
                userId = await AzureDirectoryService
                    .GetUserIdAsync(identifier)
                    .ConfigureAwait(false);
            }

            return userId.HasValue
                ? new User() { Id = userId.Value.ToString() }
                : null;
        }

        protected virtual async Task<TCommand> CreateCommandAsync<TCommand>(string name = default, Action<JObject> modifyCommandJson = default)
            where TCommand : class, IProviderCommand
        {
            var resourceName = $"{GetType().FullName}.{typeof(TCommand).Name}.json";

            if (!string.IsNullOrWhiteSpace(name))
                resourceName = Path.ChangeExtension(resourceName, $".{name.Trim()}.json");

            resourceName = GetType().Assembly.GetManifestResourceNames()
                .SingleOrDefault(n => n.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                ?? throw new TypeLoadException($"Could not load command {typeof(TCommand)} from resource '{resourceName}'");

            using var stream = GetType().Assembly.GetManifestResourceStream(resourceName);
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
            // this way the caller can modify the json before
            // its deserialized to a command object

            modifyCommandJson?.Invoke(commandJson);

            var command = commandJson.ToObject<TCommand>();

            if (command.Payload is Project project && !project.Users.Contains(commandUser))
            {
                // if this is a project scoped command and the 
                // command payload represents a project, we are
                // assigning the command user as an owner of
                // the project as default.

                project.Users.Add(commandUser.EnsureProjectMembership(project.Id, ProjectUserRole.Owner));
            }

            return command;
        }

        protected async Task<ICommandResult> SendCommandAsync(IProviderCommand providerCommand, bool waitForCallbackResult = false, TimeSpan? waitForCallbackResultTimeout = default)
        {
            if (providerCommand is null)
                throw new ArgumentNullException(nameof(providerCommand));

            await providerCommand
                .ValidateAsync(throwOnValidationError: true)
                .ConfigureAwait(false);

            var message = new ProviderCommandMessage(
                providerCommand,
                OrchestratorService.Instance.GetCallbackUrl(providerCommand));

            Logger.LogInformation($"{nameof(SendCommandAsync)}:\n{JsonConvert.SerializeObject(providerCommand, Formatting.Indented)}");

            var response = await ProviderService.BaseUrl
                .AppendPathSegment("api/command")
                .PostJsonAsync(message)
                .ConfigureAwait(false);

            var commandResult = await response.Content
                .ReadAsAsync<ICommandResult>()
                .ConfigureAwait(false);

            if (waitForCallbackResult)
            {
                commandResult = await GetCommandResultAsync(providerCommand.CommandId, waitForCallbackResult, waitForCallbackResultTimeout.GetValueOrDefault(commandResult.Timeout))
                    .ConfigureAwait(false);
            }
            else
            {
                ProviderServicePrincipalId = commandResult is ProviderRegisterCommandResult providerRegisterCommandResult
                    ? providerRegisterCommandResult.Result.PrincipalId
                    : ProviderServicePrincipalId;
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

                if (!await WaitForAsync(() => CallbackResultReceivedAsync(), timeout).ConfigureAwait(false))
                {
                    var timeoutCommandResult = await ProviderService.BaseUrl
                        .AppendPathSegment($"api/command/{commandId}")
                        .GetJsonAsync<ICommandResult>()
                        .ConfigureAwait(false);

                    var timeoutCommandErrors = timeoutCommandResult.Errors
                        .Where(err => err.Severity == CommandErrorSeverity.Error);

                    throw new TimeoutException($"Waiting for callback result ({commandId}) failed after {TimeSpan.FromMilliseconds(timeout)}",
                        timeoutCommandErrors.Skip(1).Any()
                        ? new AggregateException(timeoutCommandResult.Errors.Select(err => new Exception(err.Message)))
                        : timeoutCommandErrors.Any()
                        ? new Exception(timeoutCommandErrors.First().Message)
                        : null);
                }
            }

            var commandResult = OrchestratorService.Instance
                .GetCommandResult(commandId);

            commandResult ??= await ProviderService.BaseUrl
                .AppendPathSegment($"api/command/{commandId}")
                .GetJsonAsync<ICommandResult>()
                .ConfigureAwait(false);

            if (commandResult is ProviderRegisterCommandResult providerRegisterCommandResult)
                ProviderServicePrincipalId = providerRegisterCommandResult.Result?.PrincipalId ?? ProviderServicePrincipalId;

            Logger.LogInformation($"{nameof(GetCommandResultAsync)}:{Environment.NewLine}{JsonConvert.SerializeObject(commandResult, Formatting.Indented)}");

            return commandResult;

            async Task<bool> CallbackResultReceivedAsync()
            {
                var commandResult = OrchestratorService.Instance
                    .GetCommandResult(commandId);

                if (commandResult is null)
                {
                    commandResult = await ProviderService.BaseUrl
                        .AppendPathSegment($"api/command/{commandId}")
                        .GetJsonAsync<ICommandResult>()
                        .ConfigureAwait(false);

                    Debug.WriteLine($"Waiting for callback result {commandId} -> command runtime status: {commandResult.RuntimeStatus}");

                    return false;
                }

                return true;
            }
        }

        public virtual Task InitializeAsync()
            => Task.CompletedTask;

        public virtual Task DisposeAsync()
            => Task.CompletedTask;
    }
}
