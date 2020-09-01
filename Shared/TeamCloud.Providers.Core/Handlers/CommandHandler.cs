/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentValidation;
using Flurl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TeamCloud.Http;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Validation;
using TeamCloud.Providers.Core;
using TeamCloud.Providers.Core.Configuration;
using TeamCloud.Providers.Core.Orchestrations;

namespace TeamCloud.Providers.Azure.AppInsights
{
    public class CommandHandler
    {
        internal static string GetCommandOrchestrationInstanceId(Guid commandId)
            => commandId.ToString();

        internal static string GetCommandOrchestrationInstanceId(ICommand command)
            => GetCommandOrchestrationInstanceId(command.CommandId);

        internal static string GetCommandMessageOrchestrationInstanceId(Guid commandId)
            => $"{GetCommandOrchestrationInstanceId(commandId)}-message";

        internal static string GetCommandMessageOrchestrationInstanceId(ICommand command)
            => GetCommandMessageOrchestrationInstanceId(command.CommandId);

        private readonly IOrchestrationConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CommandHandler(IOrchestrationConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        [FunctionName(nameof(CommandHandler))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get", Route = "command/{commandId:guid?}")] HttpRequestMessage requestMessage,
            [DurableClient] IDurableClient durableClient,
            string commandId,
            ILogger log)
        {
            if (requestMessage is null)
                throw new ArgumentNullException(nameof(requestMessage));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            IActionResult actionResult;

            try
            {

                switch (requestMessage)
                {
                    case HttpRequestMessage msg when msg.Method == HttpMethod.Get:

                        if (string.IsNullOrEmpty(commandId))
                            actionResult = new NotFoundResult();
                        else
                            actionResult = await HandleGetAsync(durableClient, Guid.Parse(commandId)).ConfigureAwait(false);

                        break;

                    case HttpRequestMessage msg when msg.Method == HttpMethod.Post:

                        actionResult = await HandlePostAsync(durableClient, requestMessage, log).ConfigureAwait(false);

                        break;

                    default:
                        throw new NotSupportedException($"Http method '{requestMessage.Method}' is not supported");
                };
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Processing request failed: {requestMessage.Method.ToString().ToUpperInvariant()} {requestMessage.RequestUri}");

                throw; // re-throw exception and use the default InternalServerError behaviour 
            }

            return actionResult;
        }

        private async Task<IActionResult> HandleGetAsync(IDurableClient durableClient, Guid commandId)
        {
            var messageInstanceId = GetCommandMessageOrchestrationInstanceId(commandId);

            var messageInstanceStatus = await durableClient
                .GetStatusAsync(messageInstanceId)
                .ConfigureAwait(false);

            var message = messageInstanceStatus?.Input
                .ToObject<ProviderCommandMessage>();

            if (message?.Command is IProviderCommand command)
            {
                var providerCommandResult = await durableClient
                    .GetCommandResultAsync(command)
                    .ConfigureAwait(false);

                return CreateCommandResultResponse(command, providerCommandResult);
            }

            return new NotFoundResult();
        }

        private async Task<IActionResult> HandlePostAsync(IDurableClient durableClient, HttpRequestMessage requestMessage, ILogger log)
        {
            ProviderCommandMessage message;

            try
            {
                message = await requestMessage.Content
                    .ReadAsJsonAsync<ProviderCommandMessage>()
                    .ConfigureAwait(false);

                message.Validate(throwOnValidationError: true);
            }
            catch (ValidationException)
            {
                return new BadRequestResult();
            }

            if (message?.Command is null)
                return new BadRequestResult();

            var instanceId = GetCommandMessageOrchestrationInstanceId(message.Command.CommandId);

            try
            {
                _ = await durableClient
                    .StartNewAsync(nameof(ProviderCommandMessageOrchestration), instanceId, message)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                // check if there is an orchestration for the given
                // provider command message is already in-flight

                var commandMessageStatus = await durableClient
                    .GetStatusAsync(instanceId)
                    .ConfigureAwait(false);

                if (commandMessageStatus is null)
                    throw; // bubble exception

                return new System.Web.Http.ConflictResult();
            }

            var commandResult = await WaitForCommandResultAsync(durableClient, message.Command, log)
                .ConfigureAwait(false);

            return CreateCommandResultResponse(message.Command, commandResult);
        }

        private static async Task<ICommandResult> WaitForCommandResultAsync(IDurableClient durableClient, ICommand command, ILogger log)
        {
            var timeoutDuration = TimeSpan.FromMinutes(5);
            var timeout = DateTime.UtcNow.Add(timeoutDuration);

            while (DateTime.UtcNow <= timeout)
            {
                var commandResult = await durableClient
                    .GetCommandResultAsync(command)
                    .ConfigureAwait(false);

                if (commandResult?.RuntimeStatus.IsUnknown() ?? true)
                {
                    log.LogInformation($"Waiting for command orchestration '{command.CommandId}' ...");

                    await Task
                        .Delay(1000)
                        .ConfigureAwait(false);
                }
                else
                {
                    return commandResult;
                }
            }

            throw new TimeoutException($"Failed to get status for command {command.CommandId} within {timeoutDuration}");
        }

        private IActionResult CreateCommandResultResponse(ICommand command, ICommandResult commandResult)
        {
            if (configuration.Orchestrations.TryGetValue(command.GetType(), out var orchestrationSettings))
            {
                commandResult.Timeout = orchestrationSettings.OrchstrationTimeout?.Invoke(command)
                                        ?? OrchestrationSettings.DefaultOrchestrationTimeout(command);
            }
            else
            {
                commandResult.Timeout = OrchestrationSettings.DefaultOrchestrationTimeout(command);
            }

            if (commandResult.RuntimeStatus.IsFinal())
            {
                // this was damned fast - the orchestration already
                // finished it's work and we can return a final response.

                return new OkObjectResult(commandResult);
            }
            else
            {
                // the usual behavior - the orchestration is in progress
                // so we have to inform the caller that we accepted the command

                var location = UriHelper.GetDisplayUrl(httpContextAccessor.HttpContext.Request)
                    .AppendPathSegment(commandResult.CommandId);

                return new AcceptedResult(location, commandResult);
            }
        }
    }
}
