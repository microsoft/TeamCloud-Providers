using System;
using Microsoft.Extensions.Configuration;
using TeamCloud.Audit;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.Core.Options
{
    [Options]
    public sealed class ProviderCommandAuditOptions : ICommandAuditOptions
    {
        private readonly IConfiguration configuration;

        public ProviderCommandAuditOptions(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        string ICommandAuditOptions.ConnectionString
            => CommandAuditOptions.Default.ConnectionString;

        string ICommandAuditOptions.StoragePrefix
            => configuration.GetValue<string>("AzureFunctionsJobHost:extensions:durableTask:hubName");
    }
}
