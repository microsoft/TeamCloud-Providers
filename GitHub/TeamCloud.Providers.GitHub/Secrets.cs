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
        public static string AppCode { get; set; }

        public static GitHubAppManifest App { get; set; }

        public static InstallationWithSlug Installation { get; set; }

        public static Octokit.User Owner => App?.Owner;

        public static Octokit.User Installer { get; set; }

        public static string Log()
        {
            var serializer = new Octokit.Internal.SimpleJsonSerializer();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("");
            builder.AppendLine("Secrets:");
            builder.AppendLine($"AppCode: {AppCode}");
            builder.AppendLine($"App: {(App is null ? null : serializer.Serialize(App))}");
            builder.AppendLine($"Installation: {(Installation is null ? null : serializer.Serialize(Installation))}");
            builder.AppendLine($"Owner: {(Owner is null ? null : serializer.Serialize(Owner))}");
            builder.AppendLine($"Installer: {(Installer is null ? null : serializer.Serialize(Installer))}");
            builder.AppendLine("");

            return builder.ToString();
        }
    }
}
