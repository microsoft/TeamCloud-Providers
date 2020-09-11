﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;

namespace TeamCloud.Providers.Azure.DevOps.Data
{
    public sealed class AuthorizationToken
    {
        private static string FormatOrganizationUrl(string organization)
        {
            if (string.IsNullOrWhiteSpace(organization))
                return null;

            if (Uri.TryCreate(organization, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                return organization;

            return $"https://dev.azure.com/{organization}";
        }

        private static DateTime? GetTokenExpirationDate(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                return new JwtSecurityTokenHandler()
                    .ReadJwtToken(token)
                    .ValidTo;
            }
            catch
            {
                return null;
            }
        }

        private string organization;

        [JsonProperty("organization")]
        public string Organization
        {
            get => FormatOrganizationUrl(organization);
            set => organization = value;
        }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonIgnore]
        public DateTime? AccessTokenExpires
            => GetTokenExpirationDate(AccessToken);

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonIgnore]
        public DateTime? RefreshTokenExpires
            => GetTokenExpirationDate(RefreshToken);

    }
}
