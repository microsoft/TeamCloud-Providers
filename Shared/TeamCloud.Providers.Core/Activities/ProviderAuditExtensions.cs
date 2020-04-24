using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;

namespace TeamCloud.Providers.Core.Activities
{
    internal static class ProviderAuditExtensions
    {
        internal static Task AuditAsync(this IDurableOrchestrationContext functionContext, ICommand command, ICommandResult commandResult = default)
        { 
            if (functionContext is null)
                throw new ArgumentNullException(nameof(functionContext));

            return functionContext.CallActivityWithRetryAsync(nameof(ProviderAuditActivity), (command, commandResult));
        }
    }
}
