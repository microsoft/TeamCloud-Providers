/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Orchestration;

namespace TeamCloud.Providers.Core.Configuration
{
    public static class OrchestrationExtensions
    {
        public static IOrchestrationConfiguration MapCommand<T>(this IOrchestrationConfiguration configuration, string orchestrationName, Func<ICommand, TimeSpan> orchestrationTimeout = null)
            where T : IProviderCommand
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException("Cannot be null or empty.", nameof(orchestrationName));

            if (!FunctionsEnvironment.FunctionExists(orchestrationName))
                throw new ArgumentOutOfRangeException(nameof(orchestrationName), $"Could not find orchstration by name '{orchestrationName}'");

            configuration.Orchestrations[typeof(T)] = new OrchestrationSettings()
            {
                OrchestrationName = orchestrationName,
                OrchstrationTimeout = orchestrationTimeout ?? OrchestrationSettings.DefaultOrchestrationTimeout
            };

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
