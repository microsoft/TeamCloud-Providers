/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Azure.AppInsights.Activities
{
    public static class ProjectUpdateActivity
    {
        [FunctionName(nameof(ProjectUpdateActivity))]
        public static Dictionary<string, string> RunActivity(
            [ActivityTrigger] Project project)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            return new Dictionary<string, string>();
        }
    }
}
