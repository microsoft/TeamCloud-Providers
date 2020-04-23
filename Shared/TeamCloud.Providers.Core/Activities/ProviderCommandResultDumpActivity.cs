using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core.Activities
{
    public static class ProviderCommandResultDumpActivity
    {
        [FunctionName(nameof(ProviderCommandResultDumpActivity))]
        public static void RunActivity(
            [ActivityTrigger] ICommandResult commandResult,
            [DurableClient] IDurableClient durableClient,
            IBinder binder,
            ILogger log)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase

            if (commandResult is null)
                throw new ArgumentNullException(nameof(commandResult));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            try
            {
                var taskHubName = durableClient.GetTaskHubNameSanitized().ToLowerInvariant();

                using var commandResultWriter = binder.Bind<TextWriter>(new BlobAttribute($"{taskHubName}-commands/{commandResult.CommandId}.result.json", FileAccess.Write));

                new JsonSerializer().Serialize(commandResultWriter, commandResult);
            }
            catch (Exception exc)
            {
                log?.LogWarning(exc, $"Failed to dump command result '{commandResult.CommandId}': {JsonConvert.SerializeObject(commandResult)}");
            }

#pragma warning restore CA1308 // Normalize strings to uppercase
        }
    }
}
