/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Model.Data.Core;
using TeamCloud.Providers.Azure.DevOps.Conditional;
using TeamCloud.Providers.Testing;
using TeamCloud.Providers.Testing.Services;
using Xunit;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Azure.DevOps.Commands
{
    [Collection(nameof(ProviderContext))]
    public class ProviderProjectUpdateCommandTests : ProviderProjectCreateCommandTests
    {
        public ProviderProjectUpdateCommandTests(ProviderService providerService, ITestOutputHelper outputHelper)
            : base(providerService, outputHelper)
        { }

        [ConditionalFact(ConditionalFactPlatforms.Windows)]
        public override async Task ExecuteAsync()
        {
            await base.ExecuteAsync()
                .ConfigureAwait(false);

            var command = await CreateCommandAsync<ProviderProjectUpdateCommand>(modifyCommandJson: ModifyCommandPayload)
                .ConfigureAwait(false);

            var user = await GetUserAsync("jaysch@microsoft.com")
                .ConfigureAwait(false);

            command.Payload.Users.Add(user.EnsureProjectMembership(command.Payload.Id, ProjectUserRole.Member));

            var commandResult = await SendCommandAsync(command, true)
                .ConfigureAwait(false);

            commandResult.ShouldHaveRuntimeStatus(CommandRuntimeStatus.Completed);
        }
    }
}
