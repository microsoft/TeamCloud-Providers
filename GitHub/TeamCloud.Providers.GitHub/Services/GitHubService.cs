/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Octokit;
using Octokit.Internal;
using TeamCloud.Model.Data.Core;
using TeamCloud.Providers.GitHub.Data;

namespace TeamCloud.Providers.GitHub.Services
{
    public class GitHubService
    {
        private static Team RootTeam;
        private static Team AdminTeam;

        private GitHubClient _client;
        private GitHubAppManifest _app;

        private ProductHeaderValue ProductHeader => new ProductHeaderValue(GitHubServiceConstants.ProductHeaderName, GitHubServiceConstants.ProductHeaderVersion);

        private readonly SimpleJsonSerializer SimpleJsonSerializer = new SimpleJsonSerializer();

        private GitHubJwtFactory JwtTokenGenerator(GitHubAppManifest app) => new GitHubJwtFactory(
            new StringPrivateKeySource(app?.Pem ?? throw new InvalidOperationException("Must have GitHub App Pem key before initializing GitHub client")),
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = (int)app.Id, // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            });

        private readonly ISecretsService secretsService;

        public GitHubService(ISecretsService secretsService)
        {
            this.secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        public async Task<bool> IsConfigured()
        {
            var app = await GetAppManifest()
                .ConfigureAwait(false);

            return app != default;
        }

        private async Task<GitHubClient> GetAppClient()
        {
            if (!(_client is null))
            {
                var token = await GetToken()
                    .ConfigureAwait(false);

                if (token is null || token.ExpiresAt < DateTimeOffset.Now.AddMinutes(5))
                    _client = null;
            }

            if (_client is null)
            {
                var app = await GetAppManifest()
                    .ConfigureAwait(false);

                var jwtToken = JwtTokenGenerator(app).CreateEncodedJwtToken();

                var appClient = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
                };

                var installation = await GetInstallation()
                    .ConfigureAwait(false);

                var token = await appClient
                    .GitHubApps
                    .CreateInstallationToken(installation?.Id ?? throw new InvalidOperationException("Must have GitHub App Installation before initializing GitHub client"))
                    .ConfigureAwait(false);

                await SetSecret(token)
                    .ConfigureAwait(false);

                _client = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(token.Token)
                };
            }

            return _client;
        }

        public async Task<GitHubAppManifest> GetAppManifest(string code)
        {
            // Using Flurl as Octokit doesn't support this API yet
            // https://github.com/octokit/octokit.net/issues/2138
            var url = $"https://api.github.com/app-manifests/{code}/conversions";

            var response = await url
                .WithHeader("User-Agent", ProductHeader.ToString())
                .PostStringAsync(string.Empty);

            var json = await response.Content.ReadAsStringAsync();

            _app = SimpleJsonSerializer.Deserialize<GitHubAppManifest>(json);

            await SetSecret(_app)
                .ConfigureAwait(false);

            return _app;
        }

        public async Task<GitHubAppManifest> GetAppManifest()
        {
            if (_app is null)
                _app = await GetSecret<GitHubAppManifest>()
                    .ConfigureAwait(false);

            return _app;
        }

        public Task<InstallationWithSlug> GetInstallation()
            => GetSecret<InstallationWithSlug>();

        public Task<AccessToken> GetToken()
            => GetSecret<AccessToken>();

        private async Task<T> GetSecret<T>()
        {
            var secret = await secretsService
                .GetSecretAsync(typeof(T).Name)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(secret))
                return default;

            var item = SimpleJsonSerializer.Deserialize<T>(secret);

            return item;
        }

        private async Task SetSecret<T>(T secret)
        {
            var json = SimpleJsonSerializer.Serialize(secret);

            await secretsService
                .SetSecretAsync(typeof(T).Name, json)
                .ConfigureAwait(false);
        }

        public async Task<(Team, Repository, Octokit.Project)> CreateTeamCloudProject(Model.Data.Project project)
        {
            var team = await CreateTeam(project)
                .ConfigureAwait(false);

            var repo = await CreateRepository(project, team)
                .ConfigureAwait(false);

            var proj = await CreateProject(project, team)
                .ConfigureAwait(false);

            return (team, repo, proj);
        }

        public async Task DeleteTeamCloudProject(Model.Data.Project project)
        {
            await DeleteTeam(project)
                .ConfigureAwait(false);

            await DeleteRepository(project)
                .ConfigureAwait(false);

            await DeleteProject(project)
                .ConfigureAwait(false);
        }

        public async Task<Team> CreateTeam(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var team = await CreateTeamInternal(project.Name, Permission.Push)
                .ConfigureAwait(false);

            var members = project.Users
                .Where(u => u.IsMember(project.Id) && u.ProjectProperties(project.Id).ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
                .Select(u => (
                    login: u.ProjectProperties(project.Id).GetValueInsensitive(AvailableUserProperties.GitHubLogin),
                    role: u.IsOwner(project.Id) ? TeamRole.Maintainer : TeamRole.Member)
                );

            if (members.Any())
            {
                var tasks = members.Select(u => client
                    .Organization
                    .Team
                    .AddOrEditMembership(team.Id, u.login, new UpdateTeamMembership(u.role)
                ));

                await Task.WhenAll(tasks)
                    .ConfigureAwait(false);
            }

            await EnsureAdminUsers(project)
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

        public Task<Team> GetTeam(Model.Data.Project project, int id = default)
            => GetTeamInternal(project.Name, id);

        public async Task<Repository> CreateRepository(Model.Data.Project project, Team team)
        {
            var client = await GetAppClient().ConfigureAwait(false);
            var app = await GetAppManifest().ConfigureAwait(false);

            var gitHubProvider = project?.Type?.Providers?.FirstOrDefault(p => p.Id == "github");

            var newRepository = new NewRepository(project.Name)
            {
                AutoInit = true,
                Description = $"Repository for TeamCloud project {project.Name}",
                LicenseTemplate = gitHubProvider?.Properties.GetValueInsensitive(AvailableProperties.LicenseTemplate),
                GitignoreTemplate = gitHubProvider?.Properties.GetValueInsensitive(AvailableProperties.GitignoreTemplate)
            };

            var repository = await client
                .Repository
                .Create(app.Owner.Login, newRepository)
                .ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeam()
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

        public async Task DeleteRepository(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);
            var app = await GetAppManifest().ConfigureAwait(false);

            try
            {
                await client
                    .Repository
                    .Delete(app.Owner.Login, project.Name)
                    .ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return; // already deleted, swallow execption
            }
        }

        public async Task<Project> CreateProject(Model.Data.Project project, Team team)
        {
            var client = await GetAppClient().ConfigureAwait(false);
            var app = await GetAppManifest().ConfigureAwait(false);

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

            var adminTeam = await EnsureAdminTeam()
                .ConfigureAwait(false);

            url = new Uri($"/orgs/{app.Owner.Login}/teams/{adminTeam.Slug}/projects/{githubProject.Id}", UriKind.Relative);

            await client
                .Connection
                .Put<string>(url, body: new { Permission = "admin" }, null, accepts: GitHubServiceConstants.ProjectPreviewAcceptValue)
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

        private async Task<Team> CreateTeamInternal(string name, Permission permission)
        {
            var client = await GetAppClient().ConfigureAwait(false);
            var app = await GetAppManifest().ConfigureAwait(false);

            var rootTeam = await EnsureRootTeam()
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

        private async Task<Team> GetTeamInternal(string name, int id = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);
            var app = await GetAppManifest().ConfigureAwait(false);

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

        private async Task<Team> EnsureRootTeam()
        {
            if (RootTeam is null)
                RootTeam = await GetTeamInternal(GitHubServiceConstants.RootTeamName)
                    .ConfigureAwait(false);

            if (RootTeam is null)
            {
                // CreateTeamInternal calls this method so we cannot use it here
                var client = await GetAppClient().ConfigureAwait(false);
                var app = await GetAppManifest().ConfigureAwait(false);

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

        private async Task<Team> EnsureAdminTeam()
        {
            if (AdminTeam is null)
                AdminTeam = await GetTeamInternal(GitHubServiceConstants.AdminTeamName)
                    .ConfigureAwait(false);

            if (AdminTeam is null)
                AdminTeam = await CreateTeamInternal(GitHubServiceConstants.AdminTeamName, Permission.Admin)
                    .ConfigureAwait(false);

            return AdminTeam;
        }

        private async Task<Team> EnsureAdminUsers(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var adminTeam = await EnsureAdminTeam()
                .ConfigureAwait(false);

            var members = project.Users
                .Where(u => u.IsAdmin() && u.ProjectProperties(project.Id).ContainsKeyInsensitive(AvailableUserProperties.GitHubLogin))
                .Select(u => (
                    login: u.ProjectProperties(project.Id).GetValueInsensitive(AvailableUserProperties.GitHubLogin),
                    role: TeamRole.Member)
                );

            if (members.Any())
            {
                var tasks = members.Select(u => client
                    .Organization
                    .Team
                    .AddOrEditMembership(adminTeam.Id, u.login, new UpdateTeamMembership(u.role)
                ));

                await Task.WhenAll(tasks)
                    .ConfigureAwait(false);
            }

            return adminTeam;
        }

        private async Task<Project> GetProjectInternal(string name, int id = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);
            var app = await GetAppManifest().ConfigureAwait(false);

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

        internal Task HandleWebhook(string eventType, string payload)
            => eventType.ToLowerInvariant() switch
            {
                "ping" => HandlePing(payload),
                "installation" => HandleInstallation(payload),
                _ => HandleOther(eventType, payload)
            };

        private Task HandlePing(string payload)
        {
            var ping = SimpleJsonSerializer.Deserialize<GitHubPing>(payload);

            //log?.LogWarning($"Received GitHub Webhook: [ EventType: ping, Zen: {ping.Zen ?? "null"} ]");

            return Task.FromResult(ping);
        }

        private async Task HandleInstallation(string payload)
        {
            var hook = SimpleJsonSerializer.Deserialize<GitHubAppInstallationHook>(payload);

            await SetSecret(hook.Installation)
                .ConfigureAwait(false);

            //log?.LogWarning($"Received GitHub Webhook: [ EventType: installation, Action: {hook.Action ?? "null"} ]");
        }

        private Task HandleOther(string eventType, string payload)
        {
            var hook = SimpleJsonSerializer.Deserialize<GitHubHookPayload>(payload);

            //log?.LogWarning($"Received GitHub Webhook: [ EventType: {eventType}, Action: {hook.Action ?? "null"} ]");

            return Task.FromResult(hook);
        }
    }
}
