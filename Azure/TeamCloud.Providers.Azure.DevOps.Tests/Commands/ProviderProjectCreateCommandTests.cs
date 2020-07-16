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
        public async Task ExecuteAsync()
        {
            await AuthorizeAsync()
                .ConfigureAwait(false);

            var command = await CreateCommandAsync<ProviderProjectCreateCommand>(modifyCommandJson: ModifyCommandPayload)
                .ConfigureAwait(false);

            var commandResult = await SendCommandAsync(command)
                .ConfigureAwait(false);

            Assert.Equal(CommandRuntimeStatus.Running, commandResult.RuntimeStatus);

            commandResult = await GetCommandResultAsync(command.CommandId, true, TimeSpan.FromMinutes(5))
                .ConfigureAwait(false);

            Assert.Equal(CommandRuntimeStatus.Completed, commandResult.RuntimeStatus);

            static void ModifyCommandPayload(JObject commandJson)
            {
                (commandJson.SelectToken("$.payload.id") as JValue)?.SetValue(Guid.NewGuid());
                (commandJson.SelectToken("$.payload.name") as JValue)?.SetValue($"Project_{DateTime.UtcNow.Ticks}");
            }
        }
    }
}
