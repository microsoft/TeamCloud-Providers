/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache cache;

        public UserRegisterActivity(IAuthenticationService authenticationService, IMemoryCache cache)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this.cache = cache;
        }

        [FunctionName(nameof(UserRegisterActivity))]
        public async Task RunActivity(
            [ActivityTrigger] User user,
            ILogger log)
        {
            try
            {
                var connection = await authenticationService
                    .GetConnectionAsync()
                    .ConfigureAwait(false);

                //var identityClient = await connection
                //    .GetClientAsync<IdentityHttpClient>()
                //    .ConfigureAwait(false);

                var graphClient = await connection
                    .GetClientAsync<GraphHttpClient>()
                    .ConfigureAwait(false);

                //var licensingClient = await connection
                //    .GetClientAsync<AccountLicensingHttpClient>()
                //    .ConfigureAwait(false);

                var descriptor = await graphClient
                    .GetUserDescriptorAsync(Guid.Parse(user.Id))
                    .ConfigureAwait(false);

                if (string.IsNullOrEmpty(descriptor))
                {
                    var graphUser = await graphClient.CreateUserAsync(new GraphUserOriginIdCreationContext()
                    {
                        OriginId = user.Id

                    }).ConfigureAwait(false);

                    descriptor = graphUser.Descriptor;
                }
            }
            catch (Exception exc)
            {
                throw exc.AsSerializable();
            }
        }
    }
}
