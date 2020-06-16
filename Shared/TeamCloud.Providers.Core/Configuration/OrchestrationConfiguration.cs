/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;

namespace TeamCloud.Providers.Core.Configuration
{
    public interface IOrchestrationConfiguration
    {
        IDictionary<Type, IOrchestrationSettings> Orchestrations { get; }

        ICollection<Type> Ignored { get; }
    }

    public sealed class OrchestrationConfiguration : IOrchestrationConfiguration
    {
        internal OrchestrationConfiguration() { }

        public IDictionary<Type, IOrchestrationSettings> Orchestrations { get; }
            = new Dictionary<Type, IOrchestrationSettings>();

        public ICollection<Type> Ignored { get; }
            = new HashSet<Type>();
    }
}
