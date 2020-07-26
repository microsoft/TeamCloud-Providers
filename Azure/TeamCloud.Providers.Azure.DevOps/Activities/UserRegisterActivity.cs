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
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Graph.Client;
using TeamCloud.Model.Data;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class UserRegisterActivity
    {
        private readonly IAuthenticationService authenticationService;

        public UserRegisterActivity(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(UserRegisterActivity))]
        public async Task RunActivity(
            [ActivityTrigger] User user,
            ILogger log)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                var connection = await authenticationService
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                var graphClient = await connection
                    .GetClientAsync<GraphHttpClient>()
                    .ConfigureAwait(false);

                var descriptor = await graphClient
                    .GetUserDescriptorAsync(Guid.Parse(user.Id))
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(descriptor))
                {
                    var graphUser = await graphClient.CreateUserAsync(new GraphUserOriginIdCreationContext()
                    {
                        OriginId = user.Id

                    }).ConfigureAwait(false);

                    var token = await authenticationService
                        .GetTokenAsync()
                        .ConfigureAwait(false);

                    var userEntitlementsServiceUrl = await authenticationService
                        .GetServiceUrlAsync(ServiceEndpoint.UserEntitlements)
                        .ConfigureAwait(false);

                    var userEntitlement = new
                    {
                        user = graphUser,
                        accessLevel = new { accountLicenseType = "express" }
                    };

                    _ = await userEntitlementsServiceUrl
                        .SetQueryParam("api-version", "5.0-preview.2")
                        .WithOAuthBearerToken(token)
                        .PostJsonAsync(userEntitlement)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exc)
            {
                throw exc.AsSerializable();
            }
        }
    }
}
