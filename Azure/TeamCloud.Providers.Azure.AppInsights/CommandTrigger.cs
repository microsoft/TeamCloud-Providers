/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Validation;
using TeamCloud.Providers.Core;

namespace TeamCloud.Providers.Azure.AppInsights
{
    public class CommandTrigger
    {
        [FunctionName(nameof(CommandTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "command")] ProviderCommandMessage providerCommandMessage,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            if (providerCommandMessage is null)
                throw new ArgumentNullException(nameof(providerCommandMessage));

            providerCommandMessage.Validate(throwOnValidationError: true);

            var providerCommandResult = await durableClient
                .HandleProviderCommandMessageAsync(providerCommandMessage)
                .ConfigureAwait(false);

            return new OkObjectResult(providerCommandResult);
        }

    }
}
