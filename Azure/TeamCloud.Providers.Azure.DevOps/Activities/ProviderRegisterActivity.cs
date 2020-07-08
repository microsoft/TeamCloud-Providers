/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data.Core;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public class ProviderRegisterActivity
    {
        private readonly IAuthenticationService authenticationService;

        public ProviderRegisterActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(ProviderRegisterActivity))]
        public async Task<ProviderRegistration> RunActivityAsync(
            [ActivityTrigger] ProviderRegisterCommand command,
            ILogger log)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            using (log.BeginCommandScope(command))
            {
                try
                {
                    //var isAuthorized = await authenticationService
                    //    .IsAuthorizedAsync()
                    //    .ConfigureAwait(false);

                    //if (!isAuthorized)
                    //{
                    //    throw new CommandException($"Provider is not authorized", command);
                    //}

                    return new ProviderRegistration
                    {
                        PrincipalId = null // this provider does not talk to any azure resources yet
                    };
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
