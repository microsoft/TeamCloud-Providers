/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using TeamCloud.Providers.Testing.Services;
using Xunit;

namespace TeamCloud.Providers.Azure.DevOps
{
    [CollectionDefinition(nameof(ProviderContext))]
    public class ProviderContext : ICollectionFixture<ProviderService>
    { }
}
