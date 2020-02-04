/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers
{
    public static class Extensions
    {
        private static readonly int[] FinalRuntimeStatus = new int[]
        {
            (int) OrchestrationRuntimeStatus.Canceled,
            (int) OrchestrationRuntimeStatus.Completed,
            (int) OrchestrationRuntimeStatus.Terminated
        };

        public static ICommandResult GetResult(this DurableOrchestrationStatus orchestrationStatus)
        {
            var command = orchestrationStatus.Input.ToObject<ICommand>();

            return command.CreateResult().ApplyOrchestrationStatus(orchestrationStatus);
        }

        private static ICommandResult ApplyOrchestrationStatus(this ICommandResult commandResult, DurableOrchestrationStatus orchestrationStatus)
        {
            if (orchestrationStatus is null)
                throw new ArgumentNullException(nameof(orchestrationStatus));

            commandResult.CreatedTime = orchestrationStatus.CreatedTime;
            commandResult.LastUpdatedTime = orchestrationStatus.LastUpdatedTime;
            commandResult.RuntimeStatus = (CommandRuntimeStatus)orchestrationStatus.RuntimeStatus;
            commandResult.CustomStatus = orchestrationStatus.CustomStatus?.ToString();

            if (FinalRuntimeStatus.Contains((int)orchestrationStatus.RuntimeStatus) && (orchestrationStatus.Output?.HasValues ?? false))
            {
                var orchstrationResultType = commandResult.GetType()
                    .GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandResult<>))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault();

                if (orchstrationResultType != null)
                {
                    var orchstrationResult = orchestrationStatus.Output.ToObject(orchstrationResultType);

                    commandResult.GetType().GetProperty("Result").SetValue(commandResult, orchstrationResult);
                }
            }

            return commandResult;
        }
    }
}
