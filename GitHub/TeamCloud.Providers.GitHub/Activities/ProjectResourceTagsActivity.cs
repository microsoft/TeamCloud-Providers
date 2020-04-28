// /**
//  *  Copyright (c) Microsoft Corporation.
//  *  Licensed under the MIT License.
//  */

// using System;
// using System.Threading.Tasks;
// using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.DurableTask;
// using Microsoft.Extensions.Logging;
// using TeamCloud.Azure.Resources;
// using TeamCloud.Model;
// using TeamCloud.Model.Data;
// using TeamCloud.Serialization;

// namespace TeamCloud.Providers.GitHub.Activities
// {
//     public class ProjectResourceTagsActivity
//     {
//         private readonly IAzureResourceService azureResourceService;

//         public ProjectResourceTagsActivity(IAzureResourceService azureResourceService)
//         {
//             this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
//         }

//         [FunctionName(nameof(ProjectResourceTagsActivity))]
//         public async Task RunActivity(
//             [ActivityTrigger] IDurableActivityContext functionContext,
//             ILogger log)
//         {
//             if (functionContext is null)
//                 throw new ArgumentNullException(nameof(functionContext));

//             var (project, resourceId) = functionContext.GetInput<(Project, string)>();

//             using (log.BeginProjectScope(project))
//             {
//                 try
//                 {
//                     var resource = await azureResourceService
//                         .GetResourceAsync(resourceId, throwIfNotExists: true)
//                         .ConfigureAwait(false);

//                     await resource
//                         .SetTagsAsync(project.Tags)
//                         .ConfigureAwait(false);
//                 }
//                 catch (Exception exc)
//                 {
//                     log.LogError(exc, $"{nameof(ProjectResourceTagsActivity)} failed: {exc.Message}");

//                     throw exc.AsSerializable();
//                 }
//             }
//         }
//     }
// }
