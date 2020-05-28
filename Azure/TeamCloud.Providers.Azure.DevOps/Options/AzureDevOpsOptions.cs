using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.DevOps.Options
{
    [Options("Azure:DevOps")]
    public sealed class AzureDevOpsOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Scope { get; set; }
    }
}
