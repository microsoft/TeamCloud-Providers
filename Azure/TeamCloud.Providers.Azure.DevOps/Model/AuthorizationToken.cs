using System;
using Newtonsoft.Json;

namespace TeamCloud.Providers.Azure.DevOps.Model
{
    public sealed class AuthorizationToken : TableEntityBase
    {
        public static string[] Scopes = new string[]
        {
            "vso.agentpools", "vso.agentpools_manage",
            "vso.build", "vso.build_execute",
            "vso.code", "vso.code_write", "vso.code_manage", "vso.code_full", "vso.code_status",
            "vso.entitlements",
            "vso.memberentitlementmanagement", "vso.memberentitlementmanagement_write",
            "vso.extension", "vso.extension_manage", "vso.extension.data", "vso.extension.data_write",
            "vso.graph", "vso.graph_manage",
            "vso.identity", "vso.identity_manage",
            "vso.loadtest", "vso.loadtest_write",
            "vso.machinegroup_manage",
            "vso.gallery", "vso.gallery_acquire", "vso.gallery_publish", "vso.gallery_manage",
            "vso.notification", "vso.notification_write", "vso.notification_manage", "vso.notification_diagnostics",
            "vso.packaging", "vso.packaging_write", "vso.packaging_manage",
            "vso.project", "vso.project_write", "vso.project_manage",
            "vso.release", "vso.release_execute", "vso.release_manage",
            "vso.security_manage",
            "vso.serviceendpoint", "vso.serviceendpoint_query", "vso.serviceendpoint_manage",
            "vso.settings", "vso.settings_write",
            "vso.symbols", "vso.symbols_write", "vso.symbols_manage",
            "vso.taskgroups_read", "vso.taskgroups_write", "vso.taskgroups_manage",
            "vso.dashboards", "vso.dashboards_manage",
            "vso.test", "vso.test_write",
            "vso.tokens",
            "vso.tokenadministration",
            "vso.profile", "vso.profile_write",
            "vso.variablegroups_read", "vso.variablegroups_write", "vso.variablegroups_manage",
            "vso.wiki", "vso.wiki_write",
            "vso.work", "vso.work_write", "vso.work_full"
        };

        internal const string PartitionKeyPropertyValue = nameof(AuthorizationToken);

        public AuthorizationToken()
        {
            this.TableEntity.RowKey = Guid.NewGuid().ToString();
            this.TableEntity.PartitionKey = PartitionKeyPropertyValue;
        }

        [JsonIgnore]
        public Guid Id
        {
            get => Guid.Parse(this.TableEntity.RowKey);
            set => this.TableEntity.RowKey = value.ToString();
        }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        public bool IsPending { get; set; } = true;
    }
}

