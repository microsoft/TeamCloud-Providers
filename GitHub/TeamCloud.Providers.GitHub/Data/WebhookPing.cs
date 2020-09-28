/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

namespace TeamCloud.Providers.GitHub.Data
{
    public class WebhookPing
    {
        public string Zen { get; set; }

        public long HookId { get; set; }

        public WebhookPingHook Hook { get; set; }
    }

    public class WebhookPingHook
    {
        public long AppId { get; set; }
    }
}
