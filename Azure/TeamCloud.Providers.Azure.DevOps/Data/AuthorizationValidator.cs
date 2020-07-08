/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Flurl;
using Flurl.Http;
using TeamCloud.Http;

namespace TeamCloud.Providers.Azure.DevOps.Data
{
    public class AuthorizationValidator : AbstractValidator<AuthorizationToken>
    {
        public AuthorizationValidator()
        {
            this.CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(obj => obj)

                .Must((obj) => obj.AccessTokenExpires > DateTime.UtcNow)
                .WithMessage("Access token expired.")

                .MustAsync(async (obj, cancellationToken) =>
                {
                    try
                    {
                        _ = await GetOrganizationsAsync(obj.AccessToken)
                            .ConfigureAwait(false);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage(obj => $"Access token invalid.")

                .MustAsync(async (obj, cancellationToken) =>
                {
                    try
                    {
                        var organizations = await GetOrganizationsAsync(obj.AccessToken)
                            .ConfigureAwait(false);

                        return organizations
                            .Any(org => org.Equals(obj.Organization, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage(obj => $"Owner permissions for '{obj.Organization}' required.");

        }

        private static async Task<IEnumerable<string>> GetOrganizationsAsync(string bearerToken)
        {
            var profile = await "https://app.vssps.visualstudio.com/_apis/profile/profiles/me"
                .SetQueryParam("api-version", "4.1")
                .WithOAuthBearerToken(bearerToken)
                .AllowAnyHttpStatus()
                .GetJObjectAsync()
                .ConfigureAwait(false);

            var accounts = await "https://app.vssps.visualstudio.com/_apis/accounts"
                .SetQueryParam("ownerId", profile.SelectToken("id")?.ToString() ?? Guid.Empty.ToString())
                .SetQueryParam("api-version", "5.1")
                .WithOAuthBearerToken(bearerToken)
                .AllowAnyHttpStatus()
                .GetJObjectAsync()
                .ConfigureAwait(false);

            return accounts
                .SelectTokens("$.value[*].accountName")
                .Select(organization => $"https://dev.azure.com/{organization}");
        }
    }
}
