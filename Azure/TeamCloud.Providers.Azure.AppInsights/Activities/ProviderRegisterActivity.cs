/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public static class ProviderRegisterActivity
    {
        [FunctionName(nameof(ProviderRegisterActivity))]
        public static async Task<ProviderRegistration> RunActivity(
            [ActivityTrigger] ProviderRegisterCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            await Task.Delay(30 * 1000);

            var registration = new ProviderRegistration
            {
                PricipalId = Guid.NewGuid()
            };

            registration.Properties.Add(nameof(ProviderRegisterActivity), command.CommandId.ToString());

            return registration;
        }
    }
}
