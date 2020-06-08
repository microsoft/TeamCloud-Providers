/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Reflection;
using TeamCloud.Configuration;

namespace TeamCloud.Providers.GitHub.Options
{
    [Options]
    public class GitHubOptions
    {
        public string ProductHeaderName = "TeamCloud";

        public string ProductHeaderVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public string OrganizationName = "TeamCloudMSFT";
    }
}
