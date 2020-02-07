/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Core.Commands.Orchestrations
{
    public class ProviderCommandMessageDispatcher
    {
        private readonly IConfiguration configuration;

        public ProviderCommandMessageDispatcher(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [FunctionName(nameof(ProviderCommandMessageDispatcher))]
        public string Run(
            [ActivityTrigger] ProviderCommandMessage providerCommandMessage,
            ILogger log)
        {
            if (providerCommandMessage is null)
                throw new ArgumentNullException(nameof(providerCommandMessage));

            var commandType = providerCommandMessage.CommandType;
            var commandInterfaces = new List<Type>();

            while (commandType != null)
            {
                if (TryGetOrchestrationName(commandType, out string orchestrationNameByClassType))
                    return orchestrationNameByClassType;

                commandInterfaces
                    .AddRange(commandType.GetInterfaces());

                commandType = commandType.BaseType;
            }

            foreach (var commandInterface in commandInterfaces.Distinct())
            {
                if (TryGetOrchestrationName(commandInterface, out string orchestrationNameByInterfaceType))
                    return orchestrationNameByInterfaceType;
            }

            throw new NotSupportedException($"Command type '{providerCommandMessage.CommandType}' is not mapped to an orchestration.");

            bool TryGetOrchestrationName(Type type, out string orchestrationName)
            {
                orchestrationName = null;

                if (configuration.Orchestrations != null && configuration.Orchestrations.TryGetValue(type, out string match))
                {
                    orchestrationName = match;

                    return true;
                }

                return (configuration.Ignored?.Contains(type) ?? false);
            }
        }
    }
}
