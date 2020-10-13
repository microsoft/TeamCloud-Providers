/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using TeamCloud.Model.Data;
using TeamCloud.Providers.GitHub.Data;
using TeamCloud.Providers.GitHub.Repos.Data;
using TeamCloud.Providers.GitHub.Services;

namespace TeamCloud.Providers.GitHub.Repos.Services
{
    public class GitHubService
    {
        private static Team RootTeam;
        private static Team AdminTeam;

        private readonly SimpleJsonSerializer SimpleJsonSerializer = new SimpleJsonSerializer();

        private readonly GitHubAppService githubAppService;

        public GitHubService(GitHubAppService githubAppService)
        {
            this.githubAppService = githubAppService ?? throw new ArgumentNullException(nameof(githubAppService));
        }

        public async Task<(Team, Repository, Octokit.Project)> CreateTeamCloudProjectAsync(Model.Data.Project project)
        {
            var team = await CreateTeamAsync(project)
                .ConfigureAwait(false);

            var repo = await CreateRepositoryAsync(project, team)
                .ConfigureAwait(false);

            var proj = await CreateProjectAsync(project, team)
                .ConfigureAwait(false);

            return (team, repo, proj);
        }

        public async Task DeleteTeamCloudProjectAsync(Model.Data.Project project)
        {
            await DeleteTeamAsync(project)
                .ConfigureAwait(false);

            await DeleteRepositoryAsync(project)
                .ConfigureAwait(false);

            await DeleteProjectAsync(project)
                .ConfigureAwait(false);
        }

        public async Task<Team> CreateTeamAsync(Model.Data.Project project)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            var team = await CreateTeamInternalAsync(project.Name, Permission.Push)
                .ConfigureAwait(false);

            await SyncTeamMembershipAsync(team, project)
                .ConfigureAwait(false);

            await EnsureAdminUsersAsync(project)
                .ConfigureAwait(false);

            return team;
        }

        public async Task SyncTeamMembershipAsync(Team team, Model.Data.Project project)
        {
            if (team is null)
                throw new ArgumentNullException(nameof(team));

            if (project is null)
                throw new ArgumentNullException(nameof(project));

            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var members = project.Users
                .Where(u =>
                    u.UserType == UserType.User
                && u.IsMember(project.Id)
                && u.ProjectProperties(project.Id).ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
                .Select(u => (
                    login: u.ProjectProperties(project.Id).GetValueInsensitive(AvailableUserProperties.GitHubLogin),
                    role: u.IsOwner(project.Id) ? TeamRole.Maintainer : TeamRole.Member)
                );

            var existingMembers = await client
                .Organization
                .Team
                .GetAllMembers(team.Id)
                .ConfigureAwait(false);

            var tasks = new List<Task>();

            if (members.Any())
            {
                // var membersToAddOrUpdate = members
                //     .Where(m => !existingMembers.Any(em => em.Login.Equals(m.login, StringComparison.OrdinalIgnoreCase)));

                tasks.AddRange(members
                    .Select(m => client
                        .Organization
                        .Team
                        .AddOrEditMembership(team.Id, m.login, new UpdateTeamMembership(m.role))));

                var existingMembersToRemove = existingMembers
                    .Where(em => !members.Any(m => m.login.Equals(em.Login, StringComparison.OrdinalIgnoreCase)));

                tasks.AddRange(existingMembersToRemove
                    .Select(em => client
                        .Organization
                        .Team
                        .RemoveMembership(team.Id, em.Login)));
            }
            else if (existingMembers.Any())
            {
                tasks.AddRange(existingMembers
                    .Select(em => client
                    .Organization
                    .Team
                    .RemoveMembership(team.Id, em.Login)));
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks)
                    .ConfigureAwait(false);
            }
        }

        public async Task DeleteTeamAsync(Model.Data.Project project, int id = default)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            if (id == default)
            {
                var team = await GetTeamAsync(project)
                    .ConfigureAwait(false);

                id = team?.Id ?? default;
            }

            if (id == default)
                return; // already deleted

            try
            {
                await client
                    .Organization
                    .Team
                    .Delete(id)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return; // already deleted, swallow execption
            }
        }

        public Task<Team> GetTeamAsync(Model.Data.Project project, int id = default)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            return GetTeamInternalAsync(project.Name, id);
        }

        public async Task<Team> UpdateTeamAsync(Model.Data.Project project, int id = default)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            var team = await GetTeamInternalAsync(project.Name, id)
                .ConfigureAwait(false);

            await SyncTeamMembershipAsync(team, project)
                .ConfigureAwait(false);

            await EnsureAdminUsersAsync(project)
                .ConfigureAwait(false);

            return team;
        }

        public Task<Repository> CreateRepositoryAsync(Model.Data.Project project, Team team)
        {
            if (team is null)
                throw new ArgumentNullException(nameof(team));

            var provider = project?.Type?.Providers?.FirstOrDefault(p => p.Id == "github.repos");

            var newRepository = new NewRepository(project.Name)
            {
                AutoInit = true,
                Description = $"Repository for TeamCloud project {project.Name}",
                LicenseTemplate = provider?.Properties.GetValueInsensitive(AvailableProperties.LicenseTemplate),
                GitignoreTemplate = provider?.Properties.GetValueInsensitive(AvailableProperties.GitignoreTemplate)
            };

            return CreateRepositoryInternalAsync(team, newRepository);
        }

        public async Task<Repository> CreateRepositoryAsync(Model.Data.Project project, RepoComponentInput input)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            if (input is null)
                throw new ArgumentNullException(nameof(input));

            var team = await GetTeamInternalAsync(project.Name)
                .ConfigureAwait(false);

            var provider = project?.Type?.Providers?.FirstOrDefault(p => p.Id == "github.repos");

            var newRepository = new NewRepository(input.Name)
            {
                AutoInit = true,
                Description = input.Description ?? $"Repository for TeamCloud project {project.Name}",
                LicenseTemplate = provider?.Properties.GetValueInsensitive(AvailableProperties.LicenseTemplate),
                GitignoreTemplate = provider?.Properties.GetValueInsensitive(AvailableProperties.GitignoreTemplate)
            };

            return await CreateRepositoryInternalAsync(team, newRepository)
                .ConfigureAwait(false);
        }

        private async Task<Repository> CreateRepositoryInternalAsync(Team team, NewRepository newRepository)
        {
            if (team is null)
                throw new ArgumentNullException(nameof(team));

            if (newRepository is null)
                throw new ArgumentNullException(nameof(newRepository));

            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            var repository = await client
                .Repository
                .Create(app.Owner.Login, newRepository)
                .ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeamAsync()
                .ConfigureAwait(false);

            await client
                .Organization
                .Team
                .AddRepository(adminTeam.Id, app.Owner.Login, repository.Name, new RepositoryPermissionRequest(Permission.Admin))
                .ConfigureAwait(false);

            await client
                .Organization
                .Team
                .AddRepository(team.Id, app.Owner.Login, repository.Name, new RepositoryPermissionRequest(Permission.Push))
                .ConfigureAwait(false);

            return repository;
        }

        public Task DeleteRepositoryAsync(Model.Data.Project project)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            return DeleteRepositoryInternalAsync(project.Name);
        }

        public async Task DeleteRepositoryAsync(RepoComponent component)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            try
            {
                await client
                    .Repository
                    .Delete(component.Id)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return; // already deleted, swallow execption
            }
        }

        private async Task DeleteRepositoryInternalAsync(string name)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            try
            {
                await client
                    .Repository
                    .Delete(app.Owner.Login, name)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return; // already deleted, swallow execption
            }
        }

        public async Task<Octokit.Project> CreateProjectAsync(Model.Data.Project project, Team team)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            if (team is null)
                throw new ArgumentNullException(nameof(team));

            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            // only org level projects can be added to teams
            // repo level projects cannot
            var githubProject = await client
                .Repository
                .Project
                .CreateForOrganization(app.Owner.Login, new NewProject(project.Name)
                {
                    Body = $"Project for TeamCloud project {project.Name}"
                })
                .ConfigureAwait(false);

            var url = new Uri($"/orgs/{app.Owner.Login}/teams/{team.Slug}/projects/{githubProject.Id}", UriKind.Relative);

            await client
                .Connection
                .Put<string>(url, body: new { Permission = "write" }, null, accepts: GitHubServiceConstants.ProjectPreviewAcceptValue)
                .ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeamAsync()
                .ConfigureAwait(false);

            url = new Uri($"/orgs/{app.Owner.Login}/teams/{adminTeam.Slug}/projects/{githubProject.Id}", UriKind.Relative);

            await client
                .Connection
                .Put<string>(url, body: new { Permission = "admin" }, null, accepts: GitHubServiceConstants.ProjectPreviewAcceptValue)
                .ConfigureAwait(false);

            return githubProject;
        }

        public async Task<bool> DeleteProjectAsync(Model.Data.Project project, int id = default)
        {
            if (project is null)
                throw new ArgumentNullException(nameof(project));

            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            if (id == default)
            {
                var githubProject = await GetProjectInternalAsync(project.Name)
                    .ConfigureAwait(false);

                id = githubProject?.Id ?? default;
            }

            if (id == default)
                return false; // already deleted

            try
            {
                return await client
                    .Repository
                    .Project
                    .Delete(id)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return false; // already deleted, swallow execption
            }
        }

        private async Task<Team> CreateTeamInternalAsync(string name, Permission permission)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            var rootTeam = await EnsureRootTeamAsync()
                .ConfigureAwait(false);

            return await client
                .Organization
                .Team
                .Create(app.Owner.Login, new NewTeam(name)
                {
                    Description = name == GitHubServiceConstants.AdminTeamName ? "Team for TeamCloud Admin users." : $"Team for members of TeamCloud project {name}.",
                    ParentTeamId = rootTeam.Id,
                    Permission = permission,
                    Privacy = TeamPrivacy.Closed // Parent and nested child teams must use Closed
                })
                .ConfigureAwait(false);
        }

        private async Task<Team> GetTeamInternalAsync(string name, int id = default)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            if (id == default)
            {
                var teams = await client
                    .Organization
                    .Team
                    .GetAll(app.Owner.Login)
                    .ConfigureAwait(false);

                return teams.FirstOrDefault(t => t.Name == name);
            }

            try
            {
                return await client
                    .Organization
                    .Team
                    .Get(id)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        private async Task<Team> EnsureRootTeamAsync()
        {
            if (RootTeam is null)
                RootTeam = await GetTeamInternalAsync(GitHubServiceConstants.RootTeamName)
                    .ConfigureAwait(false);

            if (RootTeam is null)
            {
                // CreateTeamInternal calls this method so we cannot use it here
                var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

                var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

                RootTeam = await client
                    .Organization
                    .Team
                    .Create(app.Owner.Login, new NewTeam(GitHubServiceConstants.RootTeamName)
                    {
                        Description = "Parent team for all TeamCloud project teams.",
                        Privacy = TeamPrivacy.Closed // Parent and nested child teams must use Closed
                    })
                    .ConfigureAwait(false);
            }

            return RootTeam;
        }

        private async Task<Team> EnsureAdminTeamAsync()
        {
            if (AdminTeam is null)
                AdminTeam = await GetTeamInternalAsync(GitHubServiceConstants.AdminTeamName)
                    .ConfigureAwait(false);

            if (AdminTeam is null)
                AdminTeam = await CreateTeamInternalAsync(GitHubServiceConstants.AdminTeamName, Permission.Admin)
                    .ConfigureAwait(false);

            return AdminTeam;
        }

        private async Task<Team> EnsureAdminUsersAsync(Model.Data.Project project)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeamAsync()
                .ConfigureAwait(false);

            var members = project.Users
                .Where(u =>
                    u.UserType == UserType.User
                 && u.IsAdmin()
                 && u.ProjectProperties(project.Id).ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
                .Select(u => (
                    login: u.ProjectProperties(project.Id).GetValueInsensitive(AvailableUserProperties.GitHubLogin),
                    role: TeamRole.Member)
                );

            if (members.Any())
            {
                var existingMembers = await client
                    .Organization
                    .Team
                    .GetAllMembers(adminTeam.Id)
                    .ConfigureAwait(false);

                var membersToAdd = members
                    .Where(m => !existingMembers.Any(em => em.Login.Equals(m.login, StringComparison.OrdinalIgnoreCase)));

                if (membersToAdd.Any())
                {
                    var tasks = new List<Task>();

                    tasks.AddRange(membersToAdd
                        .Select(m => client
                            .Organization
                            .Team
                            .AddOrEditMembership(adminTeam.Id, m.login, new UpdateTeamMembership(m.role))));

                    await Task.WhenAll(tasks)
                        .ConfigureAwait(false);
                }
            }

            return adminTeam;
        }


        public async Task UpdateAdminUserAsync(Model.Data.User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (user.UserType == UserType.User
             && user.Properties.ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
            {
                if (user.IsAdmin())
                {
                    await EnsureAdminUserAsync(user)
                        .ConfigureAwait(false);
                }
                else
                {
                    await DeleteAdminUserAsync(user)
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task EnsureAdminUserAsync(Model.Data.User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (user.UserType == UserType.User
             && user.IsAdmin()
             && user.Properties.ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
            {
                var client = await githubAppService
                    .GetAppClientAsync()
                    .ConfigureAwait(false);

                var adminTeam = await EnsureAdminTeamAsync()
                    .ConfigureAwait(false);

                var login = user.Properties.GetValueInsensitive(AvailableUserProperties.GitHubLogin);
                try
                {
                    var member = await client
                        .Organization
                        .Team
                        .GetMembershipDetails(adminTeam.Id, login)
                        .ConfigureAwait(false);
                }
                catch (NotFoundException)
                {
                    await client
                        .Organization
                        .Team
                        .AddOrEditMembership(adminTeam.Id, login, new UpdateTeamMembership(TeamRole.Member))
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task DeleteAdminUserAsync(Model.Data.User user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            if (user.UserType == UserType.User
             && user.Properties.ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
            {
                var client = await githubAppService
                    .GetAppClientAsync()
                    .ConfigureAwait(false);

                var adminTeam = await EnsureAdminTeamAsync()
                    .ConfigureAwait(false);

                await client
                    .Organization
                    .Team
                    .RemoveMembership(adminTeam.Id, user.Properties.GetValueInsensitive(AvailableUserProperties.GitHubLogin))
                    .ConfigureAwait(false);
            }
        }

        private async Task<Octokit.Project> GetProjectInternalAsync(string name, int id = default)
        {
            var client = await githubAppService
                .GetAppClientAsync()
                .ConfigureAwait(false);

            var app = await githubAppService
                .GetAppManifestAsync()
                .ConfigureAwait(false);

            if (id == default)
            {
                var projects = await client
                    .Repository
                    .Project
                    .GetAllForOrganization(app.Owner.Login)
                    .ConfigureAwait(false);

                return projects.FirstOrDefault(t => t.Name == name);
            }

            try
            {
                return await client
                    .Repository
                    .Project
                    .Get(id)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return null;
            }
        }

        internal Task HandleWebhookAsync(string eventType, string payload, ILogger log = null)
            => eventType?.ToUpperInvariant() switch
            {
                "PING" => githubAppService.HandlePing(payload, log),
                "INSTALLATION" => githubAppService.HandleInstallationAsync(payload, log),
                _ => HandleOtherAsync(eventType, payload, log)
            };

        private Task HandleOtherAsync(string eventType, string payload, ILogger log = null)
        {
            var hook = SimpleJsonSerializer.Deserialize<WebhookPayload>(payload);

            if (!eventType.Equals("workflow_run", StringComparison.OrdinalIgnoreCase))
                log?.LogWarning($"Received GitHub Webhook: [ EventType: {eventType}, Action: {hook.Action ?? "null"} ]");
            log?.LogWarning(payload);

            return Task.FromResult(hook);
        }
    }
}
