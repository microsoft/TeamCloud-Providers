/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using TeamCloud.Model.Commands;

namespace TeamCloud.Providers.Azure.AppInsights.Orchestrations
{
    public sealed class OrchestrationRequest
    {
        public ICommand Command { get; set; }

        public string CallbackUrl { get; set; }
    }
}