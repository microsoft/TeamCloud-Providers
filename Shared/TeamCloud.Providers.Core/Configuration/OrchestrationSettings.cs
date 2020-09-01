/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Model.Commands.Core;

namespace TeamCloud.Providers.Core.Configuration
{
    public interface IOrchestrationSettings
    {
        string OrchestrationName { get; }

        Func<ICommand, TimeSpan> OrchstrationTimeout { get; set; }
    }

    public sealed class OrchestrationSettings : IOrchestrationSettings
    {
        public static readonly Func<ICommand, TimeSpan> DefaultOrchestrationTimeout = (command)
            => CommandResult.MaximumTimeout;

        internal OrchestrationSettings(string orchestrationName)
        {
            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException($"'{nameof(orchestrationName)}' cannot be null or empty", nameof(orchestrationName));

            OrchestrationName = orchestrationName;
        }

        public string OrchestrationName { get; }

        public Func<ICommand, TimeSpan> OrchstrationTimeout { get; set; }
            = DefaultOrchestrationTimeout;
    }
}
