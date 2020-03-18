/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Providers.Core.Configuration;

namespace TeamCloud.Providers.Core.Activities
{
    public class ProviderCommandDispatchActivity
    {
        private readonly IOrchestrationConfiguration configuration;

        public ProviderCommandDispatchActivity(IOrchestrationConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [FunctionName(nameof(ProviderCommandDispatchActivity))]
        public string RunActivity([ActivityTrigger] ICommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            var commandType = command.GetType();
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

            throw new NotSupportedException($"Command type '{commandType}' is not mapped to an orchestration.");

            bool TryGetOrchestrationName(Type type, out string orchestrationName)
            {
                orchestrationName = null;

                if (configuration.Orchestrations != null && configuration.Orchestrations.TryGetValue(type, out IOrchestrationSettings orchestrationSettings))
                {
                    orchestrationName = orchestrationSettings.OrchestrationName;

                    return true;
                }

                return configuration.Ignored?.Contains(type) ?? false;
            }
        }
    }
}
