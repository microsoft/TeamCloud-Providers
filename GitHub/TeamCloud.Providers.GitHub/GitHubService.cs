/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Octokit;
using GitHubJwt;
using TeamCloud.Model;
using TeamCloud.Providers.GitHub.Options;
using System.Reflection;
using Flurl.Http;

namespace TeamCloud.Providers.GitHub
{
    public class GitHubService
    {
        readonly GitHubOptions options;

        private GitHubJwtFactory JwtTokenGenerator => new GitHubJwtFactory(
            new StringPrivateKeySource(Secrets.App?.Pem ?? throw new InvalidOperationException("Must have GitHub App Pem key before initializing GitHub client")),
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = (int)Secrets.App.Id, // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            });

        public GitHubService(GitHubOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private ProductHeaderValue ProductHeader => new ProductHeaderValue(options.ProductHeaderName, options.ProductHeaderVersion);


        private GitHubClient _client;
        private DateTimeOffset? _clientExpiresAt;

        private async Task<GitHubClient> GetAppClient()
        {
            if (_client == null || !_clientExpiresAt.HasValue || _clientExpiresAt.Value < DateTimeOffset.Now)
            {
                var jwtToken = JwtTokenGenerator.CreateEncodedJwtToken();

                var appClient = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
                };

                var installation = await appClient
                    .GitHubApps
                    .GetOrganizationInstallationForCurrent(options.OrganizationName)
                    .ConfigureAwait(false);

                var token = await appClient
                    .GitHubApps
                    .CreateInstallationToken(installation.Id)
                    .ConfigureAwait(false);

                _clientExpiresAt = token.ExpiresAt;

                _client = new GitHubClient(ProductHeader)
                {
                    Credentials = new Credentials(token.Token)
                };
            }

            return _client;
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
                    // Maintainers
                    // ParentTeamId
                    // Permission
                    // Privacy
                    // RepoNames
                })
                .ConfigureAwait(false);

            return team;
        }

        public async Task<Repository> CreateRepository(Model.Data.Project project)
        {
            var client = await GetAppClient().ConfigureAwait(false);

            var repository = await client
                .Repository
                .Create(new NewRepository(project.Name)
                {
                    // LicenseTemplate = "mit",
                    // AutoInit = true,
                    // Description = "",
                    // GitignoreTemplate = "VisualStudio",
                    // TeamId = 1
                })
                .ConfigureAwait(false);

            return repository;
        }

        public async Task<Project> CreateProject(Model.Data.Project project, long repositoryId)
        {
            var client = await GetAppClient().ConfigureAwait(false);

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

        // public async Task<bool> GetManifest(string code)
        // {
        //     var url = $"https://api.github.com/app-manifests/{code}/conversions";

        //     var response = await url
        //         // .WithHeader("Accept", "application/vnd.github.fury-preview+json")
        //         // .WithHeader("Accept", "application/vnd.github.machine-man-preview+json")
        //         .WithHeader("User-Agent", $"{options.ProductHeaderName}/{options.ProductHeaderVersion}")
        //         .AllowAnyHttpStatus()
        //         .PostStringAsync("");

        //     var content response.


        // }
    }
}
