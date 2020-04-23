using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core.Activities
{
    public static class ProviderCommandDumpActivity
    {
        [FunctionName(nameof(ProviderCommandDumpActivity))]
        public static void RunActivity(
            [ActivityTrigger] ICommand command,
            [DurableClient] IDurableClient durableClient,
            IBinder binder,
            ILogger log)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase

            if (command is null)
                throw new ArgumentNullException(nameof(command));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            try
            {
                var taskHubName = durableClient.GetTaskHubNameSanitized().ToLowerInvariant();

                using var commandResultWriter = binder.Bind<TextWriter>(new BlobAttribute($"{taskHubName}-commands/{command.CommandId}.command.json", FileAccess.Write));

                new JsonSerializer().Serialize(commandResultWriter, command);
            }
            catch (Exception exc)
            {
                log?.LogWarning(exc, $"Failed to dump command result '{command.CommandId}': {JsonConvert.SerializeObject(command)}");
            }

#pragma warning restore CA1308 // Normalize strings to uppercase
        }
    }
}
