// /**
//  *  Copyright (c) Microsoft Corporation.
//  *  Licensed under the MIT License.
//  */

// using System;
// using System.Threading.Tasks;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.DurableTask;
// using Microsoft.Extensions.Logging;
// using TeamCloud.Model;
// using TeamCloud.Model.Commands;
// using TeamCloud.Model.Data;
// using TeamCloud.Orchestration;
// using TeamCloud.Providers.GitHub.Activities;
// using TeamCloud.Providers.Core;
// using TeamCloud.Serialization;

// namespace TeamCloud.Providers.GitHub.Orchestrations
// {
//     public static class ProviderRegisterOrchestration
//     {
//         [FunctionName(nameof(ProviderRegisterOrchestration))]
//         public static async Task RunOrchestration(
//             [OrchestrationTrigger] IDurableOrchestrationContext functionContext,
//             ILogger log)
//         {
//             if (functionContext is null)
//                 throw new ArgumentNullException(nameof(functionContext));

//             if (log is null)
//                 throw new ArgumentNullException(nameof(log));

//             var command = functionContext.GetInput<ProviderRegisterCommand>();
//             var commandResult = command.CreateResult();

//             using (log.BeginCommandScope(command))
//             {
//                 try
//                 {
//                     if (Guid.TryParse(command.Payload?.TeamCloudApplicationInsightsKey, out var instrumentationKey))
//                     {
//                         await functionContext
//                             .SetInstrumentationKeyAsync(instrumentationKey)
//                             .ConfigureAwait(true);
//                     }

//                     var providerRegistraion = await functionContext
//                         .CallActivityWithRetryAsync<ProviderRegistration>(nameof(ProviderRegisterActivity), command)
//                         .ConfigureAwait(true);

//                     commandResult.Result = providerRegistraion;
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
