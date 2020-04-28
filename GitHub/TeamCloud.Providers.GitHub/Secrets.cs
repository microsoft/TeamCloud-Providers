/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using TeamCloud.Providers.GitHub.Data;

namespace TeamCloud.Providers.GitHub
{
    public class Secrets
    {
        public static string ProviderUrl = "http://9c402ab9.ngrok.io";

        public static string UserCode { get; set; }

        public static string AppCode { get; set; }

        public static GitHubAppCreated App { get; set; }

        public static Octokit.User Owner => App.Owner;

        public static Octokit.User Installer { get; set; }

        public static string InstallationId { get; set; }
    }
}
