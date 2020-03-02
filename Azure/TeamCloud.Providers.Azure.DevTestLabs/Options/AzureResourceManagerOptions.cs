using System;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Azure.DevTestLabs.Options
{
    [Options("Azure:ResourceManager")]
    public class AzureResourceManagerOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string TenantId { get; set; }

        private string region;

        public string Region
        {
            get => string.IsNullOrEmpty(region) ? Environment.GetEnvironmentVariable("REGION_NAME") : region;
            set => region = value;
        }
    }
}
