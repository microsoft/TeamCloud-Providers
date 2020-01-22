/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Azure.DevOps
{
    internal static class Extensions
    {
        private static readonly int[] FinalRuntimeStatus = new int[]
        {
            (int) OrchestrationRuntimeStatus.Canceled,
            (int) OrchestrationRuntimeStatus.Completed,
            (int) OrchestrationRuntimeStatus.Terminated
        };

        internal static bool IsFinalRuntimeStatus(this DurableOrchestrationStatus status)
        {
            if (status is null) throw new ArgumentNullException(nameof(status));

            return FinalRuntimeStatus.Contains((int)status.RuntimeStatus);
        }

        internal static string GetCallbackUrl(this IHeaderDictionary headerDictionary)
        {
            return headerDictionary.TryGetValue("x-functions-callback", out var value) ? value.FirstOrDefault() : null;
        }

        internal static ICommandResult<TResult> GetResult<TResult>(this DurableOrchestrationStatus orchestrationStatus)
            where TResult : new()
        {
            var result = new CommandResult<TResult>(Guid.Parse(orchestrationStatus.InstanceId))
            {
                CreatedTime = orchestrationStatus.CreatedTime,
                LastUpdatedTime = orchestrationStatus.LastUpdatedTime,
                RuntimeStatus = (CommandRuntimeStatus)orchestrationStatus.RuntimeStatus,
                CustomStatus = orchestrationStatus.CustomStatus?.ToString(),
            };

            if (orchestrationStatus.Output?.HasValues ?? false)
            {
                result.Result = orchestrationStatus.Output.ToObject<TResult>();
            }

            return result;
        }

        internal static ICommandResult GetResult(this DurableOrchestrationStatus orchestrationStatus)
        {
            var result = new CommandResult(Guid.Parse(orchestrationStatus.InstanceId))
            {
                CreatedTime = orchestrationStatus.CreatedTime,
                LastUpdatedTime = orchestrationStatus.LastUpdatedTime,
                RuntimeStatus = (CommandRuntimeStatus)orchestrationStatus.RuntimeStatus,
                CustomStatus = orchestrationStatus.CustomStatus?.ToString(),
            };

            //if (orchestrationStatus.Output?.HasValues ?? false)
            //{
            //    result.Result = orchestrationStatus.Output.ToObject<TResult>();
            //}

            return result;
        }
    }
}
