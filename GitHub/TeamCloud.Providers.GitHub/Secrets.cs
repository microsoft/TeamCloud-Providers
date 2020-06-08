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
        public static string ProviderUrl = "";

        public static string AppCode { get; set; }

        public static GitHubAppManifest App { get; set; }

        public static InstallationWithSlug Installation { get; set; }

        public static Octokit.User Owner => App?.Owner;

        public static Octokit.User Installer { get; set; }

        // public static void InitWorkaround()
        // {
        //     if (Installation is null)
        //         throw new InvalidOperationException("Installation must be set for the workaround.");

        //     App = new GitHubAppManifest(Installation.AppId, Workaround.AppName, Installation.Account, "", Workaround.ExternalUrl, Workaround.HtmlUrl, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        //     {
        //         Slug = Installation.AppSlug,
        //         ClientId = Workaround.ClientId,
        //         ClientSecret = Workaround.ClientSecret,
        //         WebhookSecret = Workaround.WebhookSecret,
        //         Pem = Workaround.Pem
        //     };
        // }

        // public static class Workaround
        // {
        //     public static string ExternalUrl = "";
        //     public static string HtmlUrl = "";
        //     public static string AppName = "";
        //     public static string ClientId = "";
        //     public static string ClientSecret = "";
        //     public static string WebhookSecret = "";
        //     public static string Pem = "";
        // }

        public static string Log()
        {
            var serializer = new Octokit.Internal.SimpleJsonSerializer();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("");
            builder.AppendLine("Secrets:");
            builder.AppendLine($"ProviderUrl: {ProviderUrl}");
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
