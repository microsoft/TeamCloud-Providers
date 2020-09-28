/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

namespace TeamCloud.Providers.GitHub.Actions.Data
{
    public class GitHubPing
    {
        public string Zen { get; set; }

        public long HookId { get; set; }

        public GitHubAppHook Hook { get; set; }
    }

    public class GitHubAppHook
    {
        public long AppId { get; set; }
    }
}
