using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core.Orchestrations
{
    public static class ProviderCommandFallbackOrchestration
    {
        [FunctionName(nameof(ProviderCommandFallbackOrchestration))]
        public static ICommandResult RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
            ILogger log)
        {
            var command = functionContext.GetInput<IProviderCommand>();

            return command.CreateResult();
        }
    }
}
