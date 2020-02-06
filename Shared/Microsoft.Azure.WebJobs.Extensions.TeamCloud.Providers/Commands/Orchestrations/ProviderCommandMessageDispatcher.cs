/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;

namespace Microsoft.Azure.WebJobs.Extensions.TeamCloud.Providers.Commands.Orchestrations
{
    public class ProviderCommandMessageDispatcher
    {
        private readonly IConfiguration configuration;

        public ProviderCommandMessageDispatcher(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        [FunctionName(nameof(ProviderCommandMessageDispatcher))]
        public string Run(
            [ActivityTrigger] ProviderCommandMessage providerCommandMessage,
            ILogger log)
        {
            if (providerCommandMessage is null)
                throw new ArgumentNullException(nameof(providerCommandMessage));

            var commandType = providerCommandMessage.CommandType;
            var commandInterfaces = new SortedSet<Type>();

            while (commandType != null)
            {
                var types = new Type[] { commandType };

                if (configuration.Orchestrations.TryGetValue(commandType, out var orchestrationNameByClassType))
                    return orchestrationNameByClassType;

                commandType.GetInterfaces()
                    .Select(type => commandInterfaces.Add(type));

                commandType = commandType.BaseType;
            }

            foreach (var commandInterface in commandInterfaces)
            {
                if (configuration.Orchestrations.TryGetValue(commandType, out var orchestrationNameByInterfaceType))
                    return orchestrationNameByInterfaceType;
            }

            throw new ArgumentException($"Unable to resolve orchestration for command type '{providerCommandMessage.CommandType}'", nameof(providerCommandMessage));
        }
    }
}
