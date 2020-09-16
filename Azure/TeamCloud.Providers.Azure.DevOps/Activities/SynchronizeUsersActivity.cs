/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Services;
using TeamCloud.Serialization;

namespace TeamCloud.Providers.Azure.DevOps.Activities
{
    public sealed class SynchronizeUsersActivity
    {
        private readonly IAuthenticationService authenticationService;
        private readonly IDistributedCache cache;

        public SynchronizeUsersActivity(IAuthenticationService authenticationService, IDistributedCache cache)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [FunctionName(nameof(SynchronizeUsersActivity)), RetryOptions(3)]
        public async Task RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            try
            {
                using var projectClient = await authenticationService
                    .GetClientAsync<ProjectHttpClient>()
                    .ConfigureAwait(false);

                using var graphClient = await authenticationService
                    .GetClientAsync<GraphHttpClient>()
                    .ConfigureAwait(false);

                using var teamClient = await authenticationService
                    .GetClientAsync<TeamHttpClient>()
                    .ConfigureAwait(false);

                var azdoProjectId = await projectClient
                    .GetProjectIdAsync(project, cache)
                    .ConfigureAwait(false);

                var azdoProject = await projectClient
                    .GetProject(azdoProjectId.ToString())
                    .ConfigureAwait(false);

                var azdoProjectDescriptor = await graphClient
                    .GetDescriptorAsync(azdoProjectId)
                    .ConfigureAwait(false);

                await Task
                    .WhenAll
                    (
                        SyncGroupAsync(true, project.Users.Where(u => u.IsOwner(project.Id))),
                        SyncGroupAsync(false, project.Users.Where(u => u.IsMember(project.Id)))
                    )
                    .ConfigureAwait(false);

                async Task SyncGroupAsync(bool projectOwners, IEnumerable<User> users)
                {
                    var groupType = projectOwners ? "Owners" : "Members";
                    var groupName = $"TeamCloud Project {groupType}";

                    var groupsPage = await graphClient
                        .ListGroupsAsync(azdoProjectDescriptor.Value)
                        .ConfigureAwait(false);

                    var groups = groupsPage
                        .AsContinousEnumerationAsync(token => graphClient.ListGroupsAsync(azdoProjectDescriptor.Value, continuationToken: token));

                    var group = await groups
                        .SingleOrDefaultAwaitAsync(g => new ValueTask<bool>(g.Origin.Equals("vsts", StringComparison.OrdinalIgnoreCase) && g.DisplayName.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
                        .ConfigureAwait(false);

                    var currentUserDescriptors = Array.Empty<string>();

                    if (group is null)
                    {
                        log.LogInformation($"Creating group '{groupName}' in project '{azdoProject.Name}'");

                        group = await graphClient.CreateGroupAsync(new GraphGroupVstsCreationContext
                        {
                            DisplayName = groupName,
                            Description = "Managed by TeamCloud"

                        }, azdoProjectDescriptor.Value).ConfigureAwait(false);
                    }
                    else
                    {
                        var memberships = await graphClient
                            .ListMembershipsAsync(group.Descriptor, Microsoft.VisualStudio.Services.Graph.GraphTraversalDirection.Down, depth: 1)
                            .ConfigureAwait(false);

                        currentUserDescriptors = memberships
                            .Select(membership => membership.MemberDescriptor.ToString())
                            .ToArray();
                    }

                    var targetUserDescriptors = await Task
                        .WhenAll(users.Where(user => user.UserType == UserType.User).Select(user => ResolveUserDescriptorAsync(user)))
                        .ConfigureAwait(false);

                    var membershipTasks = new List<Task>();

                    membershipTasks.AddRange(targetUserDescriptors.Except(currentUserDescriptors).Select(userDescriptor => graphClient.AddMembershipAsync(userDescriptor, group.Descriptor)));
                    membershipTasks.AddRange(currentUserDescriptors.Except(targetUserDescriptors).Select(userDescriptor => graphClient.RemoveMembershipAsync(userDescriptor, group.Descriptor)));

                    await Task
                        .WhenAll(membershipTasks)
                        .ConfigureAwait(false);

                    var contributors = await groups
                        .SingleOrDefaultAwaitAsync(g => new ValueTask<bool>(g.Origin.Equals("vsts", StringComparison.OrdinalIgnoreCase) && g.DisplayName.Equals("Contributors", StringComparison.OrdinalIgnoreCase)))
                        .ConfigureAwait(false);

                    if (contributors != null)
                    {
                        var isMember = await graphClient
                            .CheckMembershipExistenceAsync(group.Descriptor, contributors.Descriptor)
                            .ConfigureAwait(false);

                        if (!isMember)
                        {
                            await graphClient
                                .AddMembershipAsync(group.Descriptor, contributors.Descriptor)
                                .ConfigureAwait(false);
                        }
                    }
                }

                async Task<string> ResolveUserDescriptorAsync(User user)
                {
                    if (user is null)
                        throw new ArgumentNullException(nameof(user));

                    try
                    {
                        var descriptor = await graphClient
                            .GetUserDescriptorAsync(Guid.Parse(user.Id), cache)
                            .ConfigureAwait(false);

                        if (string.IsNullOrEmpty(descriptor))
                        {
                            log.LogInformation($"Creating user '{user.Id}' in project '{azdoProject.Name}'");

                            var graphUser = await graphClient.CreateUserAsync(new GraphUserOriginIdCreationContext()
                            {
                                OriginId = user.Id

                            }).ConfigureAwait(false);

                            var token = await authenticationService
                                .GetTokenAsync()
                                .ConfigureAwait(false);

                            var userEntitlementsServiceUrl = await authenticationService
                                .GetServiceUrlAsync(ServiceEndpoint.UserEntitlements)
                                .ConfigureAwait(false);

                            var userEntitlement = new
                            {
                                user = graphUser,
                                accessLevel = new { accountLicenseType = "express" }
                            };

                            _ = await userEntitlementsServiceUrl
                                .SetQueryParam("api-version", "5.0-preview.2")
                                .WithOAuthBearerToken(token)
                                .PostJsonAsync(userEntitlement)
                                .ConfigureAwait(false);

                            descriptor = graphUser.Descriptor;
                        }

                        return descriptor;
                    }
                    catch (Exception exc)
                    {
                        log.LogError(exc, $"Faile to resolve user {user.Id}: {exc.Message}");

                        throw;
                    }
                }
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Synchronizing users for project {project.Id} failed: {exc.Message}");

                throw exc.AsSerializable();
            }
        }
    }
}
