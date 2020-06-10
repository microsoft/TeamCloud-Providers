/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Reflection;

namespace TeamCloud.Providers.GitHub.Services
{
    internal static class GitHubServiceConstants
    {
        internal const string AdminTeamName = "TeamCloud Admins";

        internal const string RootTeamName = "TeamCloud";

        internal const string ProjectPreviewAcceptValue = "application/vnd.github.inertia-preview+json";

        public const string ProductHeaderName = "TeamCloud";

        public static readonly string ProductHeaderVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
