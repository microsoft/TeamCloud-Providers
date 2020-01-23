/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public static class ProjectUpdateActivity
    {
        [FunctionName(nameof(ProjectUpdateActivity))]
        public static async Task<Result> RunOrchestration(
            [ActivityTrigger] ProjectUpdateCommand command,
            ILogger logger)
        {
            logger.LogInformation($"Processing Command: {JsonConvert.SerializeObject(command)}");

            if (command is null)
                throw new ArgumentNullException(nameof(command));

            await Task.Delay(2 * 60 * 1000);

            var randomResult = new Result();
            randomResult.Variables.Add(nameof(ProjectUpdateActivity), command.ProjectId?.ToString());

            return randomResult;
        }

        public class Result
        {
            public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        }
    }
}