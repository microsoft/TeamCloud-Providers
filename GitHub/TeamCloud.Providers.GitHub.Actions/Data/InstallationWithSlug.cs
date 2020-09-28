/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using Octokit;

namespace TeamCloud.Providers.GitHub.Actions.Data
{
    public class InstallationWithSlug : Installation
    {
        public string AppSlug { get; set; }
    }
}
