/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Reflection;

namespace TeamCloud.Providers.GitHub.Services
{
    internal static class GitHubServiceConstants
    {
        public const string ProductHeaderName = "TeamCloud";

        public static readonly string ProductHeaderVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
