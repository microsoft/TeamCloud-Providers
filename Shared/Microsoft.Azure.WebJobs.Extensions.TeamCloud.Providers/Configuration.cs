/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using TeamCloud.Model.Commands;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers
{
    public interface IConfiguration
    {
        string ConnectionString { get; }

        IReadOnlyDictionary<Type, string> Orchestrations { get; }
    }

    public sealed class Configuration : IConfiguration
    {
        private readonly Dictionary<Type, string> orchestrations = new Dictionary<Type, string>();

        internal Configuration() { }

        public string ConnectionString { get; set; }
            = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        public void Map<T>(string orchestrationName)
            where T : ICommand
        {
            if (string.IsNullOrEmpty(orchestrationName))
                throw new ArgumentException("Cannot be null or empty.", nameof(orchestrationName));

            orchestrations[typeof(T)] = orchestrationName;
        }

        IReadOnlyDictionary<Type, string> IConfiguration.Orchestrations
            => orchestrations;
    }
}
