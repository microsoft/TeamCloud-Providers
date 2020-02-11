/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Core
{
    public interface IConfiguration
    {
        string ConnectionString { get; }

        IReadOnlyDictionary<Type, string> Orchestrations { get; }

        IReadOnlyCollection<Type> Ignored { get; }
    }

    public sealed class Configuration : IConfiguration
    {
        private readonly Dictionary<Type, string> orchestrations = new Dictionary<Type, string>();

        private readonly HashSet<Type> ignores = new HashSet<Type>();

        internal Configuration() { }

        public string ConnectionString { get; set; }
            = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        public Configuration MapCommand<T>(string orchestrationName)
            where T : ICommand
        {
            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException("Cannot be null or empty.", nameof(orchestrationName));

            orchestrations[typeof(T)] = orchestrationName;

            return this;
        }

        public Configuration IgnoreCommand<T>()
            where T : ICommand
        {
            ignores.Add(typeof(T));

            return this;
        }

        IReadOnlyDictionary<Type, string> IConfiguration.Orchestrations
            => orchestrations;

        IReadOnlyCollection<Type> IConfiguration.Ignored
            => ignores;
    }
}
