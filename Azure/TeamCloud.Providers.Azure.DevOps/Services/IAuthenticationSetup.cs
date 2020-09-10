/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using TeamCloud.Providers.Azure.DevOps.Data;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public interface IAuthenticationSetup
    {
        Task SetupAsync(AuthorizationToken authorizationToken);
    }
}
