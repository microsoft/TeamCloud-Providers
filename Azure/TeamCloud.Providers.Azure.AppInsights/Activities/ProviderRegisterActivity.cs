/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Azure;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public class ProviderRegisterActivity
    {
        private readonly IAzureSessionService azureSessionService;

        public ProviderRegisterActivity(IAzureSessionService azureSessionService)
        {
            this.azureSessionService = azureSessionService ?? throw new ArgumentNullException(nameof(azureSessionService));
        }

        [FunctionName(nameof(ProviderRegisterActivity))]
        public async Task<ProviderRegistration> RunActivity(
            [ActivityTrigger] ProviderRegisterCommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            try
            {
                var identity = await azureSessionService
                    .GetIdentityAsync()
                    .ConfigureAwait(false);

                var registration = new ProviderRegistration
                {
                    PrincipalId = identity?.ObjectId
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
