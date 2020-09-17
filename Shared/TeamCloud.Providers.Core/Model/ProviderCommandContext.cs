/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Specialized;
using Newtonsoft.Json;
using TeamCloud.Model.Commands;
using TeamCloud.Serialization.Converter;

namespace TeamCloud.Providers.Core.Model
{
    public sealed class ProviderCommandContext
    {
        public IProviderCommand Command { get; set; }

        [JsonConverter(typeof(NameValueCollectionConverter))]
        public NameValueCollection Context { get; set; }
            = new NameValueCollection();
    }
}
