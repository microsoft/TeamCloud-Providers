using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Core.Activities
{
    public static class ProviderCommandPayloadActivity
    {
        [FunctionName(nameof(ProviderCommandPayloadActivity))]
        public static void RunActivity([ActivityTrigger] ICommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            if (command.Payload is Project project)
            {

            }

        }
    }
}
