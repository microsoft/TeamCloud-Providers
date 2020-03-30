/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public static class ProviderRegisterActivity
    {
        [FunctionName(nameof(ProviderRegisterActivity))]
        public static ProviderRegistration RunActivity(
            [ActivityTrigger] ProviderRegisterCommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            try
            {
                var registration = new ProviderRegistration
                {
                    PrincipalId = null // this provider does not talk to any azure resources yet
                };

                return registration;
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableException))
            {
                throw serializableException;
            }
        }
    }
}
