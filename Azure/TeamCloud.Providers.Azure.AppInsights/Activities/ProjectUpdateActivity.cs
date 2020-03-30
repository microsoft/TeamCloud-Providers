/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Data;
using TeamCloud.Serialization;

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

            try
            {
                return new Dictionary<string, string>();
            }
            catch (Exception exc) when (!exc.IsSerializable(out var serializableException))
            {
                throw serializableException;
            }
        }
    }
}
