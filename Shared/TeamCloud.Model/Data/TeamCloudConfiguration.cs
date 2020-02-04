/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TeamCloud.Model.Data
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public sealed class TeamCloudConfiguration
    {
        public List<ProjectType> ProjectTypes { get; set; } = new List<ProjectType>();

        public List<Provider> Providers { get; set; } = new List<Provider>();

        public List<User> Users { get; set; } = new List<User>();

        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public sealed class TeamCloudConfigurationValidator : AbstractValidator<TeamCloudConfiguration>
    {
        public TeamCloudConfigurationValidator()
        {
            //RuleFor(obj => obj.Version).NotEmpty();
            RuleFor(obj => obj.ProjectTypes).NotEmpty();
            RuleFor(obj => obj.Providers).NotEmpty();
            RuleFor(obj => obj.Users).NotEmpty();

            // there must at least one user with role admin
            RuleFor(obj => obj.Users).Must(users => users.Any(u => u.Role == UserRoles.TeamCloud.Admin))
                .WithMessage($"There must be at least one user with the role '{UserRoles.TeamCloud.Admin}'.");
        }
    }
}
