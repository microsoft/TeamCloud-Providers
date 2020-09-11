using System;

namespace TeamCloud.Providers.Azure.DevOps.Services
{
    public static class ServiceEndpointExtensions
    {
        public static string ToUrl(this ServiceEndpoint serviceEndpointUrl, string organization)
        {
            if (string.IsNullOrWhiteSpace(organization))
                throw new ArgumentException($"'{nameof(organization)}' cannot be null or whitespace", nameof(organization));

            return serviceEndpointUrl switch
            {
                ServiceEndpoint.Root => $"https://dev.azure.com/{organization}/_apis",
                ServiceEndpoint.UserEntitlements => $"https://vsaex.dev.azure.com/{organization}/_apis/userentitlements",
                _ => throw new NotSupportedException($"Service endpoint of type '{serviceEndpointUrl}' is not supported"),
            };
        }
    }
}
