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
using TeamCloud.Providers.Core;

namespace TeamCloud.Providers.Azure.DevOps
{
    public class CommandTrigger
    {
        [FunctionName(nameof(CommandTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "command")] ProviderCommandMessage providerCommandMessage,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            if (providerCommandMessage is null)
                throw new ArgumentNullException(nameof(providerCommandMessage));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var providerCommandResult = await durableClient
                .HandleProviderCommandMessageAsync(providerCommandMessage)
                .ConfigureAwait(false);

            if (providerCommandResult.RuntimeStatus.IsFinal())
            {
                return new OkObjectResult(providerCommandResult);
            }

            return new AcceptedResult(string.Empty, providerCommandResult);
        }
    }
}
