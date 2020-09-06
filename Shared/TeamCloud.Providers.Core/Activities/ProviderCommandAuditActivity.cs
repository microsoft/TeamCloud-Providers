using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Audit;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core
{
    public sealed class ProviderCommandAuditActivity
    {
        private readonly ICommandAuditWriter commandAuditWriter;

        public ProviderCommandAuditActivity(ICommandAuditWriter commandAuditWriter = null)
        {
            this.commandAuditWriter = commandAuditWriter ?? new CommandAuditWriter();
        }

        [FunctionName(nameof(ProviderCommandAuditActivity))]
        public async Task RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            ILogger logger)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            try
            {
                var (command, commandResult, providerId) =
                    functionContext.GetInput<(ICommand, ICommandResult, string)>();

                await commandAuditWriter
                    .AuditAsync(command, commandResult, providerId)
                    .ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, $"Command auditing failed: {exc.Message}");
            }
        }
    }
}
