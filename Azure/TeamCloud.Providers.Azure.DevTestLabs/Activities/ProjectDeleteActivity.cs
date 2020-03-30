/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Commands;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevTestLabs.Activities
{
    public static class ProjectDeleteActivity
    {
        [FunctionName(nameof(ProjectDeleteActivity))]
        public static Dictionary<string, string> RunActivity(
            [ActivityTrigger] ProviderProjectDeleteCommand command)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

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
