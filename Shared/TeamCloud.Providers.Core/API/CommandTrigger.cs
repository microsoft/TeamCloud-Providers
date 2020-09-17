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
using TeamCloud.Providers.Core.Configuration;
using TeamCloud.Providers.Core.Model;
using TeamCloud.Providers.Core.Orchestrations;

namespace TeamCloud.Providers.Core.API
{
    public sealed class CommandTrigger
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

        public CommandTrigger(IOrchestrationConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        [FunctionName(nameof(CommandTrigger))]
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
            var command = await durableClient
                .GetCommandAsync(commandId)
                .ConfigureAwait(false);

            if (command != null)
            {
                var commandResult = await durableClient
                    .GetCommandResultAsync(command)
                    .ConfigureAwait(false);

                commandResult ??= command.CreateResult();

                return CreateCommandResultResponse(command, commandResult);
            }

            return new NotFoundResult();
        }

        private async Task<IActionResult> HandlePostAsync(IDurableClient durableClient, HttpRequestMessage requestMessage, ILogger log)
        {
            ProviderCommandMessage commandMessage;

            try
            {
                commandMessage = await requestMessage.Content
                    .ReadAsJsonAsync<ProviderCommandMessage>()
                    .ConfigureAwait(false);

                if (commandMessage?.Command is null)
                    return new BadRequestResult();

                commandMessage.Validate(throwOnValidationError: true);
            }
            catch (ValidationException)
            {
                return new BadRequestResult();
            }

            var instanceId = GetCommandMessageOrchestrationInstanceId(commandMessage.Command.CommandId);

            try
            {
                log.LogInformation($"Starting provider command message orchestration for command {commandMessage.Command.CommandId}");

                var commandContext = new ProviderCommandContext()
                {
                    Command = commandMessage.Command,
                    Context = requestMessage.RequestUri.ParseQueryString()
                };

                _ = await durableClient
                    .StartNewAsync(nameof(ProviderCommandMessageOrchestration), instanceId, (commandMessage, commandContext))
                    .ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                if (exc is InvalidOperationException invalidOperationException)
                {
                    // check if there is an orchestration for the given
                    // provider command message is already in-flight

                    var commandMessageStatus = await durableClient
                        .GetStatusAsync(instanceId)
                        .ConfigureAwait(false);

                    if (commandMessageStatus != null)
                    {
                        log.LogWarning(exc, $"Provider command message orchestration for command {commandMessage.Command.CommandId} already started: {exc.Message}");

                        return new System.Web.Http.ConflictResult();
                    }
                }

                log.LogError(exc, $"Failed to start provider command message orchestration for command {commandMessage.Command.CommandId}: {exc.Message}");

                throw;
            }

            var commandResult = await durableClient
                .GetCommandResultAsync(commandMessage.Command)
                .ConfigureAwait(false);

            commandResult ??= commandMessage.Command.CreateResult();

            return CreateCommandResultResponse(commandMessage.Command, commandResult);
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
                return new OkObjectResult(commandResult);

            var location = httpContextAccessor.HttpContext.Request.GetDisplayUrl();

            if (!location.EndsWith(commandResult.CommandId.ToString(), StringComparison.OrdinalIgnoreCase))
                location = location.AppendPathSegment(commandResult.CommandId);

            return new AcceptedResult(location, commandResult);
        }
    }
}
