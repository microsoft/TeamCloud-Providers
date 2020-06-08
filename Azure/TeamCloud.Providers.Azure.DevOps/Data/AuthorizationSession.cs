using System;
using Newtonsoft.Json;

namespace TeamCloud.Providers.Azure.DevOps.Data
{
    public sealed class AuthorizationSession : TableEntityBase
    {
        public static string[] Scopes = new string[]
        {
            "vso.analytics",
            "vso.auditlog",
            "vso.build_execute",
            "vso.code_full",
            "vso.code_status",
            "vso.connected_server",
            "vso.dashboards_manage",
            "vso.entitlements",
            "vso.environment_manage",
            "vso.extension.data_write",
            "vso.extension_manage",
            "vso.gallery_acquire",
            "vso.gallery_manage",
            "vso.graph_manage",
            "vso.identity_manage",
            "vso.loadtest_write",
            "vso.machinegroup_manage",
            "vso.memberentitlementmanagement_write",
            "vso.notification_diagnostics",
            "vso.notification_manage",
            "vso.packaging_manage",
            "vso.profile_write",
            "vso.project_manage",
            "vso.release_manage",
            "vso.securefiles_manage",
            "vso.security_manage",
            "vso.serviceendpoint_manage",
            "vso.symbols_manage",
            "vso.taskgroups_manage",
            "vso.test_write",
            "vso.tokenadministration",
            "vso.tokens",
            "vso.variablegroups_manage",
            "vso.wiki_write",
            "vso.work_full"
        };

        internal const string PartitionKeyPropertyValue = nameof(AuthorizationSession);

        public AuthorizationSession()
        {
            TableEntity.RowKey = Guid.NewGuid().ToString();
            TableEntity.PartitionKey = PartitionKeyPropertyValue;
        }

        [JsonIgnore]
        public Guid Id
        {
            get => Guid.Parse(TableEntity.RowKey);
            set => TableEntity.RowKey = value.ToString();
        }

        public string Organization { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }


        public AuthorizationToken ToAuthorizationToken()
            => new AuthorizationToken()
            {
                Organization = Organization,
                ClientId = ClientId,
                ClientSecret = ClientSecret
            };
    }
}

