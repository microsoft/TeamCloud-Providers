/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Reflection;

namespace TeamCloud.Providers.GitHub.Actions.Services
{
    internal static class GitHubServiceConstants
    {
        internal const string ProjectPreviewAcceptValue = "application/vnd.github.inertia-preview+json";

        public const string ProductHeaderName = "TeamCloud";

        public static readonly string ProductHeaderVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
