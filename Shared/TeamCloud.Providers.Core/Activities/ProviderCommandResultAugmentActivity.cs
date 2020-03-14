using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Core.Activities
{
    public static class ProviderCommandResultAugmentActivity
    {
        [FunctionName(nameof(ProviderCommandResultAugmentActivity))]
        public static async Task<ICommandResult> RunActivity(
            [ActivityTrigger] IDurableActivityContext functionContext,
            [DurableClient] IDurableClient durableClient)
        {
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            if (durableClient is null)
                throw new ArgumentNullException(nameof(durableClient));

            var (commandResult, instanceId) = functionContext.GetInput<(ICommandResult, string)>();

            try
            {
                var commandStatus = await durableClient
                    .GetStatusAsync(instanceId)
                    .ConfigureAwait(false);

                return commandResult
                    .ApplyStatus(commandStatus);
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableExc))
            {
                throw serializableExc;
            }
        }
    }
}
