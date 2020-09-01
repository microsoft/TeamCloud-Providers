/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;

namespace TeamCloud.Providers.Core.Configuration
{
    public static class OrchestrationExtensions
    {
        public static IOrchestrationConfiguration SubscribeEvent(this IOrchestrationConfiguration configuration, ProviderEventSubscription providerEventSubscription)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            if (providerEventSubscription is null)
                throw new ArgumentNullException(nameof(providerEventSubscription));

            if (!configuration.Orchestrations.ContainsKey(typeof(ProviderEventCommand)))
                throw new NotSupportedException($"Unable to subscribe if no {typeof(ProviderEventCommand)} was mapped to an orchestration.");

            if (providerEventSubscription.Equals(ProviderEventSubscription.All))
                configuration.Subscriptions.Clear();

            configuration.Subscriptions.Add(providerEventSubscription);

            return configuration;
        }

        public static IOrchestrationConfiguration MapCommand<T>(this IOrchestrationConfiguration configuration, string orchestrationName, Action<IOrchestrationSettings> orchestrationSettings = null)
            where T : IProviderCommand
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException("Cannot be null or empty.", nameof(orchestrationName));

            if (!FunctionsEnvironment.FunctionExists(orchestrationName))
                throw new ArgumentOutOfRangeException(nameof(orchestrationName), $"Could not find orchstration by name '{orchestrationName}'");

            var settings = configuration.Orchestrations[typeof(T)] = new OrchestrationSettings(orchestrationName);

            orchestrationSettings?.Invoke(settings);

            return configuration;
        }

        public static IOrchestrationConfiguration IgnoreCommand<T>(this IOrchestrationConfiguration configuration)
            where T : IProviderCommand
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            configuration.Ignored.Add(typeof(T));

            return configuration;
        }
    }
}
