/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public static class SendCommandResultActivity
    {
        [FunctionName(nameof(SendCommandResultActivity))]
        public static async Task<bool> RunActivity(
            [ActivityTrigger] ProviderCommand providerCommand,
            [DurableClient] IDurableClient durableClient,
            ILogger log)
        {
            if (providerCommand is null)
                throw new ArgumentNullException(nameof(providerCommand));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            try
            {
                var status = await durableClient
                    .GetStatusAsync(providerCommand.CommandId.ToString(), showHistory: false, showHistoryOutput: false, showInput: false)
                    .ConfigureAwait(false);

                var providerCommandResult = providerCommand.CreateResult(status);

                if (providerCommandResult.RuntimeStatus.IsFinal())
                {
                    var response = await providerCommand.CallbackUrl
                        .PostJsonAsync(providerCommandResult)
                        .ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                log.LogError(ex, "SendCommandResultActivity Failed");
                throw;
            }
        }
    }
}
