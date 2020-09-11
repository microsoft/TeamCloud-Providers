/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using TeamCloud.Http;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Data;
using TeamCloud.Providers.Azure.DevOps.Services;

namespace TeamCloud.Providers.Azure.DevOps.Handlers
{
    public sealed class AuthorizationHandler
    {
        private const string VisualStudioAuthUrl = "https://app.vssps.visualstudio.com/oauth2/authorize";
        private const string VisualStudioTokenUrl = "https://app.vssps.visualstudio.com/oauth2/token";

        private readonly IAuthenticationService authenticationService;

        private static async Task<string> GetBadRequestErrorDescriptionAsync(HttpResponseMessage httpResponseMessage)
        {
            try
            {
                var json = await httpResponseMessage
                    .ReadAsJsonAsync()
                    .ConfigureAwait(false);

                return json?.SelectToken("$..ErrorDescription")?.ToString() ?? json.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static async Task<AuthorizationSession> GetAuthorizationSessionAsync(CloudTable sessionTable, Guid sessionId)
        {
            if (sessionTable is null)
                throw new ArgumentNullException(nameof(sessionTable));

            var sessionResult = await sessionTable
                .ExecuteAsync(TableOperation.Retrieve<AuthorizationSession>(AuthorizationSession.PartitionKeyPropertyValue, sessionId.ToString()))
                .ConfigureAwait(false);

            if (sessionResult.HttpStatusCode == (int)HttpStatusCode.OK)
                return sessionResult.Result as AuthorizationSession;

            return null;
        }

        private static async Task<AuthorizationSession> SetAuthorizationSessionAsync(CloudTable sessionTable, AuthorizationSession session)
        {
            if (sessionTable is null)
                throw new ArgumentNullException(nameof(sessionTable));

            if (session is null)
                throw new ArgumentNullException(nameof(session));

            var sessionResult = await sessionTable
                .ExecuteAsync(TableOperation.InsertOrReplace(session))
                .ConfigureAwait(false);

            return sessionResult.Result as AuthorizationSession;
        }

        private static Task ClearAuthorizationSessionAsync(CloudTable sessionTable, AuthorizationSession session)
        {
            if (sessionTable is null)
                throw new ArgumentNullException(nameof(sessionTable));

            if (session is null)
                throw new ArgumentNullException(nameof(session));

            return sessionTable
                    .ExecuteAsync(TableOperation.Delete(session));
        }

        private static Task<string> GetAuthorizeUrl()
            => FunctionsEnvironment.GetFunctionUrlAsync(nameof(AuthorizationHandler));

        private static Task<string> GetCallbackUrl()
            => FunctionsEnvironment.GetFunctionUrlAsync(nameof(AuthorizationHandler) + nameof(Callback));

        public static async Task<AuthorizationToken> RefreshAsync(AuthorizationToken token)
        {
            if (token is null)
                throw new ArgumentNullException(nameof(token));

            if (string.IsNullOrEmpty(token.ClientSecret))
                throw new ArgumentException("Missing client secret.", nameof(token));

            if (token.RefreshTokenExpires.GetValueOrDefault(DateTime.UtcNow) <= DateTime.UtcNow)
                throw new ArgumentException("Refresh token expired.", nameof(token));

            var form = new
            {
                client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                client_assertion = token.ClientSecret,
                grant_type = "refresh_token",
                assertion = token.RefreshToken,
                redirect_uri = await GetCallbackUrl().ConfigureAwait(false)
            };

            var response = await VisualStudioTokenUrl
                .WithHeaders(new MediaTypeWithQualityHeaderValue("application/json"))
                .AllowAnyHttpStatus()
                .PostUrlEncodedAsync(form)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                JsonConvert.PopulateObject(json, token);
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await GetBadRequestErrorDescriptionAsync(response)
                    .ConfigureAwait(false);

                token = null;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }

            return token;
        }

        public AuthorizationHandler(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        [FunctionName(nameof(AuthorizationHandler))]
        public Task<IActionResult> Authorize(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "authorize")] HttpRequestMessage requestMessage,
            [Table(nameof(AuthorizationSession))] CloudTable sessionTable)
        {
            if (requestMessage is null)
                throw new ArgumentNullException(nameof(requestMessage));

            if (sessionTable is null)
                throw new ArgumentNullException(nameof(sessionTable));

            return requestMessage.Method switch
            {
                HttpMethod m when m == HttpMethod.Get => AuthorizeGet(requestMessage),
                HttpMethod m when m == HttpMethod.Post => AuthorizePost(requestMessage, sessionTable),
                _ => throw new NotImplementedException(),
            };
        }

        private Task<IActionResult> AuthorizeGet(HttpRequestMessage requestMessage)
        {
            var queryString = requestMessage.RequestUri.ParseQueryString();
            var queryError = queryString.GetValues("error")?.FirstOrDefault()?.UrlDecode();

            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{this.GetType().FullName}.html");

            using var streamReader = new StreamReader(stream);

            return Task.FromResult<IActionResult>(new ContentResult
            {
                StatusCode = (int)HttpStatusCode.OK,
                ContentType = "text/html",
                Content = ReplaceTokens(streamReader.ReadToEnd())
            });

            string ReplaceTokens(string content) => Regex.Replace(content, "{@(\\w+)}", (match) => match.Groups[1].Value switch
            {
                "Error" => queryError,
                "ApplicationWebsite" => FunctionsEnvironment.GetHostUrlAsync().SyncResult(),
                "ApplicationCallback" => FunctionsEnvironment.GetFunctionUrlAsync(nameof(AuthorizationHandler) + nameof(Callback)).SyncResult(),
                "Organization" => authenticationService.GetOrganizationUrlAsync().SyncResult() ?? string.Empty,
                "ClientId" => string.Empty,
                _ => match.Value
            });
        }

        private async Task<IActionResult> AuthorizePost(HttpRequestMessage requestMessage, CloudTable sessionTable)
        {
            var payload = await requestMessage.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            var fields = HttpUtility
                .ParseQueryString(payload)
                .ToDictionary();

            var session = await SetAuthorizationSessionAsync(sessionTable, new AuthorizationSession()
            {
                Organization = fields.GetValueOrDefault("organization")?.SingleOrDefault(),
                ClientId = fields.GetValueOrDefault("client_id")?.SingleOrDefault(),
                ClientSecret = fields.GetValueOrDefault("client_secret")?.SingleOrDefault()

            }).ConfigureAwait(false);

            var queryParams = HttpUtility.ParseQueryString(string.Empty);

            queryParams["client_id"] = fields.GetValueOrDefault("client_id")?.SingleOrDefault();
            queryParams["response_type"] = "Assertion";
            queryParams["state"] = session.Id.ToString();
            queryParams["scope"] = string.Join(' ', AuthorizationSession.Scopes);
            queryParams["redirect_uri"] = await GetCallbackUrl().ConfigureAwait(false);

            var uriBuilder = new UriBuilder(VisualStudioAuthUrl)
            {
                Query = queryParams.ToString()
            };

            return new RedirectResult(uriBuilder.ToString());
        }


        [FunctionName(nameof(AuthorizationHandler) + nameof(Callback))]
        public async Task<ActionResult> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "authorize/callback")] HttpRequestMessage requestMessage,
            [Table(nameof(AuthorizationSession))] CloudTable sessionTable,
            ILogger log)
        {
            if (requestMessage is null)
                throw new ArgumentNullException(nameof(requestMessage));

            if (sessionTable is null)
                throw new ArgumentNullException(nameof(sessionTable));

            string error;

            try
            {
                var queryParams = requestMessage.RequestUri.ParseQueryString();

                if (!queryParams.TryGetValue("error", out error) &&
                    queryParams.TryGetValue("code", out string code) && !string.IsNullOrEmpty(code) &&
                    queryParams.TryGetValue("state", out string state) && Guid.TryParse(state, out Guid sessionId))
                {
                    var session = await GetAuthorizationSessionAsync(sessionTable, sessionId)
                        .ConfigureAwait(false);

                    if (session is null)
                    {
                        error = "Authorization session invalid";
                    }
                    else
                    {
                        try
                        {
                            var form = new
                            {
                                client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                                client_assertion = session.ClientSecret,
                                grant_type = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                                assertion = queryParams.Get("code"),
                                redirect_uri = await GetCallbackUrl().ConfigureAwait(false)
                            };

                            var response = await VisualStudioTokenUrl
                                .WithHeaders(new MediaTypeWithQualityHeaderValue("application/json"))
                                .AllowAnyHttpStatus()
                                .PostUrlEncodedAsync(form)
                                .ConfigureAwait(false);

                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);

                                var token = session.ToAuthorizationToken();

                                JsonConvert.PopulateObject(json, token);

                                if (authenticationService is IAuthenticationSetup authenticationSetup)
                                {
                                    await authenticationSetup
                                        .SetAsync(token)
                                        .ConfigureAwait(false);
                                }
                            }
                            else if (response.StatusCode == HttpStatusCode.BadRequest)
                            {
                                error = await GetBadRequestErrorDescriptionAsync(response)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                error = response.ReasonPhrase;
                            }
                        }
                        finally
                        {
                            await ClearAuthorizationSessionAsync(sessionTable, session)
                                .ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    error = "Bad request";
                }
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Processing authorization callback failed: {exc.Message}");

                error = exc.Message;
            }

            var authorizationUrl = await GetAuthorizeUrl()
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(error))
            {
                authorizationUrl = authorizationUrl
                    .SetQueryParam("succeeded", null, Flurl.NullValueHandling.NameOnly);
            }
            else
            {
                authorizationUrl = authorizationUrl
                    .SetQueryParam("error", error);
            }

            return new RedirectResult(authorizationUrl);
        }
    }
}