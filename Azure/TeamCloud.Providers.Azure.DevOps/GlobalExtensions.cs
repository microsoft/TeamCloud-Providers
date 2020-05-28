using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace TeamCloud.Providers.Azure.DevOps
{
    internal static class GlobalExtensions
    {
        public static Dictionary<string, string[]> ToDictionary(this NameValueCollection collection)
            => collection.Cast<string>().ToDictionary(key => key, key => collection.GetValues(key));
        public static string ToQueryString(this NameValueCollection nvc)
        {
            if (nvc == null) return string.Empty;

            var sb = new StringBuilder();

            foreach (string key in nvc.Keys)
            {
                var values = nvc.GetValues(key);

                if (string.IsNullOrWhiteSpace(key) || values is null)
                    continue;

                foreach (string value in values)
                {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
                }
            }

            return sb.ToString();
        }
    }
}
