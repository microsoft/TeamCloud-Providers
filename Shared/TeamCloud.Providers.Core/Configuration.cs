/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Core
{
    public interface IOrchestrationConfiguration
    {
        string ConnectionString { get; }

        IReadOnlyDictionary<Type, string> Orchestrations { get; }

        IReadOnlyCollection<Type> Ignored { get; }
    }

    public sealed class OrchestrationConfiguration : IOrchestrationConfiguration
    {
        private readonly Dictionary<Type, string> orchestrations = new Dictionary<Type, string>();

        private readonly HashSet<Type> ignores = new HashSet<Type>();

        internal OrchestrationConfiguration() { }

        public string ConnectionString { get; set; }
            = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        public OrchestrationConfiguration MapCommand<T>(string orchestrationName)
            where T : ICommand
        {
            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException("Cannot be null or empty.", nameof(orchestrationName));

            orchestrations[typeof(T)] = orchestrationName;

            return this;
        }

        public OrchestrationConfiguration IgnoreCommand<T>()
            where T : ICommand
        {
            ignores.Add(typeof(T));

            return this;
        }

        IReadOnlyDictionary<Type, string> IOrchestrationConfiguration.Orchestrations
            => orchestrations;

        IReadOnlyCollection<Type> IOrchestrationConfiguration.Ignored
            => ignores;
    }
}
