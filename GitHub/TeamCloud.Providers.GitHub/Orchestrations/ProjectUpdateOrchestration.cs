// /**
//  *  Copyright (c) Microsoft Corporation.
//  *  Licensed under the MIT License.
//  */

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.DurableTask;
// using Microsoft.Extensions.Logging;
// using TeamCloud.Model;
// using TeamCloud.Model.Commands;
// using TeamCloud.Orchestration;
// using TeamCloud.Providers.GitHub.Activities;
// using TeamCloud.Serialization;

// namespace TeamCloud.Providers.GitHub.Orchestrations
// {
//     public static class ProjectUpdateOrchestration
//     {
//         [FunctionName(nameof(ProjectUpdateOrchestration))]
//         public static async Task RunOrchestration(
//             [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
//             ILogger log)
//         {
//             if (functionContext is null)
//                 throw new ArgumentNullException(nameof(functionContext));

//             if (log is null)
//                 throw new ArgumentNullException(nameof(log));

//             var command = functionContext.GetInput<ProviderProjectUpdateCommand>();
//             var commandResult = command.CreateResult();

//             using (log.BeginCommandScope(command))
//             {
//                 try
//                 {
//                     var resources = await functionContext
//                         .CallActivityWithRetryAsync<IEnumerable<string>>(nameof(ProjectResourceListActivity), command.Payload)
//                         .ConfigureAwait(true);

//                     var tasks = new List<Task>();

//                     tasks.AddRange(resources.Select(resource => functionContext.CallActivityWithRetryAsync(nameof(ProjectResourceRolesActivity), (command.Payload, resource))));
//                     tasks.AddRange(resources.Select(resource => functionContext.CallActivityWithRetryAsync(nameof(ProjectResourceTagsActivity), (command.Payload, resource))));

//                     await Task
//                         .WhenAll(tasks)
//                         .ConfigureAwait(true);
//                 }
//                 catch (Exception exc)
//                 {
//                     commandResult ??= command.CreateResult();
//                     commandResult.Errors.Add(exc);

//                     throw exc.AsSerializable();
//                 }
//                 finally
//                 {
//                     var commandException = commandResult.GetException();

//                     if (commandException is null)
//                         functionContext.SetCustomStatus($"Command succeeded", log);
//                     else
//                         functionContext.SetCustomStatus($"Command failed: {commandException.Message}", log, commandException);

//                     functionContext.SetOutput(commandResult);
//                 }
//             }
//         }
//     }
// }
