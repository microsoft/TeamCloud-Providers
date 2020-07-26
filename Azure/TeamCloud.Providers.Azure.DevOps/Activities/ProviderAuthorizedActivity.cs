/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public class ProviderAuthorizedActivity
    {
        private readonly IAuthenticationService authenticationService;

        public ProviderAuthorizedActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(ProviderAuthorizedActivity))]
        public async Task RunActivityAsync(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger log)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            try
            {
                var isAuthorized = await authenticationService
                    .IsAuthorizedAsync()
                    .ConfigureAwait(false);

                if (!isAuthorized)
                    throw new RetryCanceledException($"Provider not authorized");
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"{nameof(ProviderRegisterActivity)} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }

}
