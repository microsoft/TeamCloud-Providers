/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Newtonsoft.Json;
using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.GitHub.Data
{
    public class RepositoryDispatchEvent
    {
        [JsonProperty("event_type")]
        public string EventType { get; set; }


        [JsonProperty("client_payload")]
        public IProviderCommand ClientPayload { get; set; }
    }
}
