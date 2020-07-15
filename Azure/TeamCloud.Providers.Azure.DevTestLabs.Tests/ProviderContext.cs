/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using TeamCloud.Providers.Testing.Services;
using Xunit;

namespace TeamCloud.Providers.Azure.DevTestLabs
{
    [CollectionDefinition(Name)]
    public sealed class ProviderContext : ICollectionFixture<ProviderService>
    {
        public const string Name = nameof(ProviderContext);
    }
}
