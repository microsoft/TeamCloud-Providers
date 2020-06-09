

using System.Reflection;
/**
*  Copyright (c) Microsoft Corporation.
*  Licensed under the MIT License.
*/
namespace TeamCloud.Providers.GitHub
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
