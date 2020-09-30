/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Core.Configuration
{
    public interface IOrchestrationConfiguration
    {
        ISet<string> RequiredQueryParams { get; }

        IDictionary<Type, IOrchestrationSettings> Orchestrations { get; }

        ISet<ProviderEventSubscription> Subscriptions { get; }

        ISet<Type> Ignored { get; }
    }

    public sealed class OrchestrationConfiguration : IOrchestrationConfiguration
    {
        internal OrchestrationConfiguration() { }

        public ISet<string> RequiredQueryParams { get; }
            = new HashSet<string>();

        public IDictionary<Type, IOrchestrationSettings> Orchestrations { get; }
            = new Dictionary<Type, IOrchestrationSettings>();

        public ISet<ProviderEventSubscription> Subscriptions { get; }
            = new HashSet<ProviderEventSubscription>();

        public ISet<Type> Ignored { get; }
            = new HashSet<Type>();
    }
}
