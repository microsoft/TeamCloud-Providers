/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Xunit;

namespace TeamCloud.Providers.Azure.DevOps
{
    [CollectionDefinition(nameof(ProviderContext))]
    public class ProviderContext : ICollectionFixture<ProviderService>
    { }
}
