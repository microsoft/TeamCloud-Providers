/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Providers.Azure.AppInsights.Orchestrations;
using Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers;

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
