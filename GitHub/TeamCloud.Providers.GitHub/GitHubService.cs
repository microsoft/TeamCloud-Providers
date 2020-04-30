/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Octokit;
using TeamCloud.Providers.GitHub.Data;
using TeamCloud.Providers.GitHub.Options;

namespace TeamCloud.Providers.GitHub
{
    public class GitHubService
    {
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

        public async Task CreateTeamCloudProject(Model.Data.Project project)
        {
            var team = await CreateTeam(project)
                .ConfigureAwait(false);

            var repository = await CreateRepository(project, team.Id)
                .ConfigureAwait(false);

            _ = await CreateProject(project, repository.Id)
                .ConfigureAwait(false);
        }

        public async Task DeleteTeamCloudProject(Model.Data.Project project)
        {
            await DeleteTeam(project)
                .ConfigureAwait(false);

            await DeleteRepository(project)
                .ConfigureAwait(false);

            // I assume it'll be implicitly deleted by deleting the repo
            // var githubProject = await DeleteProject(project)
            //     .ConfigureAwait(false);
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

        public async Task<Team> CreateTeam(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var team = await client
                .Organization
                .Team
                .Create(options.OrganizationName, new NewTeam(project.Name)
                {
                    // Description
                    // Maintainers = new List<string> { Secrets.Installer.Login }
                    // ParentTeamId
                    // Permission
                    // Privacy
                    // RepoNames
                })
                .ConfigureAwait(false);

            return team;
        }

        public async Task DeleteTeam(Model.Data.Project project, int teamId = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (teamId == default)
            {
                var teams = await client
                    .Organization
                    .Team
                    .GetAll(Secrets.Owner.Login)
                    .ConfigureAwait(false);

                var team = teams.FirstOrDefault(t => t.Name == project.Name);

                teamId = team.Id;
            }

            await client
                .Organization
                .Team
                .Delete(teamId)
                .ConfigureAwait(false);
        }

        public async Task<Repository> CreateRepository(Model.Data.Project project, int teamId = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var newRepository = new NewRepository(project.Name)
            {
                AutoInit = true,
                Description = "",
                LicenseTemplate = "mit", // TODO: use provider property
                GitignoreTemplate = "VisualStudio" // TODO: use provider property
            };

            if (teamId != default)
                newRepository.TeamId = teamId;

            var repository = await client
                .Repository
                .Create(Secrets.Owner.Login, newRepository)
                .ConfigureAwait(false);

            return repository;
        }

        public async Task DeleteRepository(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            await client
                .Repository
                .Delete(Secrets.Owner.Login, project.Name)
                .ConfigureAwait(false);
        }

        public async Task<Project> CreateProject(Model.Data.Project project, long repositoryId = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (repositoryId == default)
            {
                var repository = await client
                    .Repository
                    .Get(Secrets.Owner.Login, project.Name)
                    .ConfigureAwait(false);

                repositoryId = repository.Id;
            }

            var githubProject = await client
                .Repository
                .Project
                .CreateForRepository(repositoryId, new NewProject(project.Name)
                {
                    // Body
                })
                .ConfigureAwait(false);

            return githubProject;
        }

        public async Task<bool> DeleteProject(Model.Data.Project project, int githubProjectId = default)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            if (githubProjectId == default)
            {
                var githubProjects = await client
                    .Repository
                    .Project
                    .GetAllForRepository(Secrets.Owner.Login, project.Name)
                    .ConfigureAwait(false);

                var githubProject = githubProjects.FirstOrDefault(p => p.Name == project.Name);

                githubProjectId = githubProject.Id;
            }

            var deleted = await client
                .Repository
                .Project
                .Delete(githubProjectId)
                .ConfigureAwait(false);

            return deleted;
        }
    }
}
