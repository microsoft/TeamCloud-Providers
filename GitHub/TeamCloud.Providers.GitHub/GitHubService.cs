/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using TeamCloud.Model.Data;
using TeamCloud.Providers.GitHub.Data;
using TeamCloud.Providers.GitHub.Options;

namespace TeamCloud.Providers.GitHub
{
    public class GitHubService
    {
        private const string AdminTeamName = "TeamCloud Admins";
        private const string RootTeamName = "TeamCloud";

        private static Team RootTeam;
        private static Team AdminTeam;

        private GitHubClient _client;

        private DateTimeOffset? _clientExpiresAt;

        private ProductHeaderValue ProductHeader => new ProductHeaderValue(options.ProductHeaderName, options.ProductHeaderVersion);

        private GitHubJwtFactory JwtTokenGenerator => new GitHubJwtFactory(
            new StringPrivateKeySource(Secrets.App?.Pem ?? throw new InvalidOperationException("Must have GitHub App Pem key before initializing GitHub client")),
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = (int)Secrets.App.Id, // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            });

        readonly GitHubOptions options;

        public GitHubService(GitHubOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private async Task<GitHubClient> GetAppClient()
        {
            if (_client == null || !_clientExpiresAt.HasValue || _clientExpiresAt.Value < DateTimeOffset.Now)
            {
                var jwtToken = JwtTokenGenerator.CreateEncodedJwtToken();

                var appClient = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
                };

                var token = await appClient
                    .GitHubApps
                    .CreateInstallationToken(Secrets.Installation?.Id ?? throw new InvalidOperationException("Must have GitHub App Installation before initializing GitHub client"))
                    .ConfigureAwait(false);

                _clientExpiresAt = token.ExpiresAt;

                _client = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(token.Token)
                };
            }

            return _client;
        }


        public async Task<GitHubAppManifest> GetManifest(string code)
        {
            Secrets.AppCode = code;

            // Using Flurl as Octokit doesn't support this API yet
            // https://github.com/octokit/octokit.net/issues/2138
            var url = $"https://api.github.com/app-manifests/{code}/conversions";

            var response = await url
                .WithHeader("User-Agent", ProductHeader.ToString())
                .PostStringAsync(string.Empty);

            var json = await response.Content.ReadAsStringAsync();

            var serializer = new Octokit.Internal.SimpleJsonSerializer();
            var app = serializer.Deserialize<GitHubAppManifest>(json);

            Secrets.App = app;

            return app;
        }


        public async Task<(Team, Repository, Octokit.Project)> CreateTeamCloudProject(Model.Data.Project project, ILogger log)
        {
            log?.LogWarning("Creating GitHub Team...");
            var team = await CreateTeam(project, log)
                .ConfigureAwait(false);

            log?.LogWarning("Creating GitHub Repository");
            var repo = await CreateRepository(project, team, log)
                .ConfigureAwait(false);

            log?.LogWarning("Creating GitHub Project");
            var proj = await CreateProject(project, team, log)
                .ConfigureAwait(false);

            return (team, repo, proj);
        }


        public async Task DeleteTeamCloudProject(Model.Data.Project project, ILogger log)
        {
            log?.LogWarning("Deleting GitHub Team");
            await DeleteTeam(project)
                .ConfigureAwait(false);

            log?.LogWarning("Deleting GitHub Repository");
            await DeleteRepository(project)
                .ConfigureAwait(false);

            log?.LogWarning("Deleting GitHub Project");
            await DeleteProject(project)
                .ConfigureAwait(false);
        }

        public async Task<Team> CreateTeam(Model.Data.Project project, ILogger log)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var team = await CreateTeamInternal(project.Name, Permission.Push, log)
                .ConfigureAwait(false);

            var members = project.Users
                .Where(u => u.IsMember(project.Id) && u.ProjectProperties(project.Id).ContainsKey("GitHubLogin"))
                .Select(u => (login: u.ProjectProperties(project.Id)["GitHubLogin"], role: u.IsOwner(project.Id) ? TeamRole.Maintainer : TeamRole.Member));

            if (members.Any())
            {
                var tasks = members.Select(u => client
                    .Organization
                    .Team
                    .AddOrEditMembership(team.Id, u.login, new UpdateTeamMembership(u.role)
                ));

                log?.LogWarning($"Adding Memberships to Team: {team.Name}");
                await Task.WhenAll(tasks)
                    .ConfigureAwait(false);
            }

            await EnsureAdminUsers(project, log)
                .ConfigureAwait(false);

            return team;
        }

        public async Task DeleteTeam(Model.Data.Project project, int id = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (id == default)
            {
                var team = await GetTeam(project)
                    .ConfigureAwait(false);

                id = team?.Id ?? default;
            }

            if (id == default)
                return; // already deleted

            await client
                .Organization
                .Team
                .Delete(id)
                .ConfigureAwait(false);
        }

        public Task<Team> GetTeam(Model.Data.Project project, int id = default)
            => GetTeamInternal(project.Name, id);

        public async Task<Repository> CreateRepository(Model.Data.Project project, Team team, ILogger log)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var newRepository = new NewRepository(project.Name)
            {
                AutoInit = true,
                Description = "",
                LicenseTemplate = "mit", // TODO: use provider property
                GitignoreTemplate = "VisualStudio" // TODO: use provider property
            };

            log?.LogWarning($"Creating Repository: {newRepository.Name}");
            var repository = await client
                .Repository
                .Create(options.OrganizationName, newRepository)
                .ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeam(log)
                .ConfigureAwait(false);

            log?.LogWarning($"Adding Repository '{newRepository.Name}' to Team: {adminTeam.Name}");
            await client
                .Organization
                .Team
                .AddRepository(adminTeam.Id, options.OrganizationName, repository.Name, new RepositoryPermissionRequest(Permission.Admin))
                .ConfigureAwait(false);

            log?.LogWarning($"Adding Repository '{newRepository.Name}' to Team: {team.Name}");
            await client
                .Organization
                .Team
                .AddRepository(team.Id, options.OrganizationName, repository.Name, new RepositoryPermissionRequest(Permission.Push))
                .ConfigureAwait(false);

            return repository;
        }

        public async Task DeleteRepository(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            await client
                .Repository
                .Delete(options.OrganizationName, project.Name)
                .ConfigureAwait(false);
        }

        public async Task<Octokit.Project> CreateProject(Model.Data.Project project, Team team, ILogger log)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            // only org level projects can be added to teams
            // repo level projects cannot
            log?.LogWarning($"Creating Project '{project.Name}'");
            var githubProject = await client
                .Repository
                .Project
                .CreateForOrganization(options.OrganizationName, new NewProject(project.Name))
                .ConfigureAwait(false);

            var url = new Uri($"/orgs/{options.OrganizationName}/teams/{team.Slug}/projects/{githubProject.Id}", UriKind.Relative);

            log?.LogWarning($"Adding Project '{githubProject.Name}' to Team '{team.Name}'");
            await client
                .Connection
                .Put(url, accepts: "application/vnd.github.inertia-preview+json")
                // .Put<string>(url, body: new { Permission = "write" }, accepts: "application/vnd.github.inertia-preview+json")
                .ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeam(log)
                .ConfigureAwait(false);

            url = new Uri($"/orgs/{options.OrganizationName}/teams/{adminTeam.Slug}/projects/{githubProject.Id}", UriKind.Relative);

            log?.LogWarning($"Adding Project '{githubProject.Name}' to Team '{adminTeam.Name}'");
            await client
                .Connection
                .Put(url, accepts: "application/vnd.github.inertia-preview+json")
                // .Put<string>(url, body: new { Permission = "admin" })
                .ConfigureAwait(false);

            return githubProject;
        }

        public async Task<bool> DeleteProject(Model.Data.Project project, int id = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (id == default)
            {
                var githubProject = await GetProjectInternal(project.Name)
                    .ConfigureAwait(false);

                id = githubProject?.Id ?? default;
            }

            if (id == default)
                return false; // already deleted

            var deleted = await client
                .Repository
                .Project
                .Delete(id)
                .ConfigureAwait(false);

            return deleted;
        }


        private async Task<Team> CreateTeamInternal(string name, Permission permission, ILogger log)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            log?.LogWarning("Ensuring Root Team");
            var rootTeam = await EnsureRootTeam()
                .ConfigureAwait(false);
            log?.LogWarning($"Found or created Root Team with id: {rootTeam.Id}");

            log?.LogWarning($"Creating New Team: {name}");
            return await client
                .Organization
                .Team
                .Create(options.OrganizationName, new NewTeam(name)
                {
                    ParentTeamId = rootTeam.Id,
                    Permission = permission,
                    Privacy = TeamPrivacy.Closed // Parent and nested child teams must use Closed
                })
                .ConfigureAwait(false);
        }

        private async Task<Team> GetTeamInternal(string name, int id = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (id == default)
            {
                var teams = await client
                    .Organization
                    .Team
                    .GetAll(options.OrganizationName)
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

        private async Task<Team> EnsureRootTeam()
        {
            if (RootTeam is null)
                RootTeam = await GetTeamInternal(RootTeamName)
                    .ConfigureAwait(false);

            if (RootTeam is null)
            {
                // CreateTeamInternal calls this method so we cannot use
                // it here
                var client = await GetAppClient().ConfigureAwait(false);

                RootTeam = await client
                    .Organization
                    .Team
                    .Create(options.OrganizationName, new NewTeam(RootTeamName)
                    {
                        Privacy = TeamPrivacy.Closed // Parent and nested child teams must use Closed
                    })
                    .ConfigureAwait(false);
            }

            return RootTeam;
        }

        // TODO: Admin users

        private async Task<Team> EnsureAdminTeam(ILogger log)
        {
            log?.LogWarning("Ensuring Admin Team");

            if (AdminTeam is null)
                AdminTeam = await GetTeamInternal(AdminTeamName)
                    .ConfigureAwait(false);

            if (AdminTeam is null)
                AdminTeam = await CreateTeamInternal(AdminTeamName, Permission.Admin, log)
                    .ConfigureAwait(false);

            return AdminTeam;
        }


        private async Task<Team> EnsureAdminUsers(Model.Data.Project project, ILogger log)
        {
            log?.LogWarning("Ensuring Admin Team Users");

            var client = await GetAppClient().ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeam(log)
                .ConfigureAwait(false);

            var members = project.Users
                .Where(u => u.IsAdmin() && u.ProjectProperties(project.Id).ContainsKey("GitHubLogin"))
                .Select(u => (login: u.ProjectProperties(project.Id)["GitHubLogin"], role: TeamRole.Member));

            if (members.Any())
            {
                var tasks = members.Select(u => client
                    .Organization
                    .Team
                    .AddOrEditMembership(adminTeam.Id, u.login, new UpdateTeamMembership(u.role)
                ));

                log?.LogWarning($"Adding Memberships to Team: {adminTeam.Name}");
                await Task.WhenAll(tasks)
                    .ConfigureAwait(false);
            }

            return adminTeam;
        }

        private async Task<Octokit.Project> GetProjectInternal(string name, int id = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (id == default)
            {
                var projects = await client
                    .Repository
                    .Project
                    .GetAllForOrganization(options.OrganizationName)
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
    }
}
