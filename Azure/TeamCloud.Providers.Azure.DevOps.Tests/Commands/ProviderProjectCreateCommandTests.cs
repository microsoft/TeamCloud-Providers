/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Providers.Azure.DevOps.Conditional;
using TeamCloud.Providers.Testing;
using TeamCloud.Providers.Testing.Services;
using Xunit;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Azure.DevOps.Commands
{
    [Collection(nameof(ProviderContext))]
    public class ProviderProjectCreateCommandTests : ProviderCommandDevOpsTests
    {
        public ProviderProjectCreateCommandTests(ProviderService providerService, ITestOutputHelper outputHelper)
            : base(providerService, outputHelper)
        { }

        [ConditionalFact(ConditionalFactPlatforms.Windows)]
        public virtual async Task ExecuteAsync()
        {
            await AuthorizeAsync()
                .ConfigureAwait(false);

            var command = await CreateCommandAsync<ProviderProjectCreateCommand>(modifyCommandJson: ModifyCommandPayload)
                .ConfigureAwait(false);

            var commandResult = await SendCommandAsync(command, true)
                .ConfigureAwait(false);

            commandResult.ShouldHaveRuntimeStatus(CommandRuntimeStatus.Completed);
        }
    }
}
