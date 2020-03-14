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

        Func<ICommand, TimeSpan> OrchstrationTimeout { get; }
    }

    public sealed class OrchestrationSettings : IOrchestrationSettings
    {
        internal static readonly Func<ICommand, TimeSpan> DefaultOrchestrationTimeout = (command)
            => CommandResult.MaximumTimeout;

        internal OrchestrationSettings()
        { }

        public string OrchestrationName { get; internal set; }

        public Func<ICommand, TimeSpan> OrchstrationTimeout { get; internal set; }
    }
}
