/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

namespace TeamCloud.Providers.GitHub.Repos.Data
{
    public class RepoComponent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Private { get; set; }
        public string FullName { get; set; }
        public bool IsTemplate { get; set; }
        public string Description { get; set; }
        public string DefaultBranch { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string CloneUrl { get; set; }
        public string GitUrl { get; set; }
        public string SshUrl { get; set; }
        public string SvnUrl { get; set; }
        public string MirrorUrl { get; set; }
    }
}
