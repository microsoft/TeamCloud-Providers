/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.GitHub.Activities
{
    public class ProviderRegisterActivity
    {
        [FunctionName(nameof(ProviderRegisterActivity))]
        public static ProviderRegistration RunActivity(
            [ActivityTrigger] ProviderRegisterCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            using (log.BeginCommandScope(command))
            {
                try
                {
                    var registration = new ProviderRegistration
                    {
                        PrincipalId = null, // this provider does not talk to any azure resources yet
                        CommandMode = ProviderCommandMode.Extended
                    };

                    return registration;
                }
                catch (Exception exc)
                {
                    log.LogError(exc, $"{nameof(ProviderRegisterActivity)} failed: {exc.Message}");

                    throw exc.AsSerializable();
                }
            }
        }
    }
}
