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
        string ConnectionString { get; }

        IReadOnlyDictionary<Type, IOrchestrationSettings> Orchestrations { get; }

        IReadOnlyCollection<Type> Ignored { get; }
    }

    public sealed class OrchestrationConfiguration : IOrchestrationConfiguration
    {
        internal OrchestrationConfiguration() { }

        public string ConnectionString { get; set; }
            = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        public OrchestrationConfiguration MapCommand<T>(string orchestrationName, Func<ICommand, TimeSpan> orchestrationTimeout = null)
            where T : IProviderCommand
        {
            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException("Cannot be null or empty.", nameof(orchestrationName));

            if (!FunctionsEnvironment.FunctionExists(orchestrationName))
                throw new ArgumentOutOfRangeException(nameof(orchestrationName), $"Could not find orchstration by name '{orchestrationName}'");

            orchestrations[typeof(T)] = new OrchestrationSettings()
            {
                OrchestrationName = orchestrationName,
                OrchstrationTimeout = orchestrationTimeout ?? OrchestrationSettings.DefaultOrchestrationTimeout
            };

            return this;
        }

        public OrchestrationConfiguration IgnoreCommand<T>()
            where T : IProviderCommand
        {
            ignores.Add(typeof(T));

            return this;
        }

        private readonly Dictionary<Type, IOrchestrationSettings> orchestrations = new Dictionary<Type, IOrchestrationSettings>();

        IReadOnlyDictionary<Type, IOrchestrationSettings> IOrchestrationConfiguration.Orchestrations => orchestrations;

        private readonly HashSet<Type> ignores = new HashSet<Type>();

        IReadOnlyCollection<Type> IOrchestrationConfiguration.Ignored => ignores;
    }
}
