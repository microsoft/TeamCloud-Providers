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
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using TeamCloud.Http;
using TeamCloud.Orchestration;
using TeamCloud.Providers.Azure.DevOps.Model;

namespace TeamCloud.Providers.Azure.DevOps.Handlers
{
    public sealed class AuthorizationHandler
    {
        private const string AuthUrl = "https://app.vssps.visualstudio.com/oauth2/authorize";
        private const string TokenUrl = "https://app.vssps.visualstudio.com/oauth2/token";

        private static async Task<AuthorizationToken> GetAuthorizationTokenAsync(CloudTable authorizationTokenTable, Guid sessionId)
        {
            if (authorizationTokenTable is null)
                throw new ArgumentNullException(nameof(authorizationTokenTable));

            var authorizationTokenResult = await authorizationTokenTable
                .ExecuteAsync(TableOperation.Retrieve(AuthorizationToken.PartitionKeyPropertyValue, sessionId.ToString()))
                .ConfigureAwait(false);

            if (authorizationTokenResult.HttpStatusCode == (int)HttpStatusCode.OK)
                return authorizationTokenResult.Result as AuthorizationToken;

            return null;
        }

        private static async Task<AuthorizationToken> SetAuthorizationTokenAsync(CloudTable authorizationTokenTable, AuthorizationToken session)
        {
            if (authorizationTokenTable is null)
                throw new ArgumentNullException(nameof(authorizationTokenTable));

            if (session is null)
                throw new ArgumentNullException(nameof(session));

            var authorizationTokenResult = await authorizationTokenTable
                .ExecuteAsync(TableOperation.InsertOrReplace(session))
                .ConfigureAwait(false);

            return authorizationTokenResult.Result as AuthorizationToken;
        }


        [FunctionName(nameof(AuthorizationHandler))]
        public Task<IActionResult> Authorize(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "authorize")] HttpRequestMessage requestMessage,
            [Table(nameof(AuthorizationToken))] CloudTable authorizationTokenTable)
            => requestMessage.Method switch
            {
                HttpMethod m when m == HttpMethod.Get => AuthorizeGet(authorizationTokenTable, requestMessage),
                HttpMethod m when m == HttpMethod.Post => AuthorizePost(authorizationTokenTable, requestMessage),
                _ => throw new NotImplementedException(),
            };

        private Task<IActionResult> AuthorizeGet(CloudTable authorizationTokenTable, HttpRequestMessage requestMessage)
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"{this.GetType().FullName}.html");

            return Task.FromResult<IActionResult>(new ContentResult
            {
                StatusCode = (int)HttpStatusCode.OK,
                ContentType = "text/html",
                Content = ReplaceTokens(new StreamReader(stream).ReadToEnd())
            });

            string ReplaceTokens(string content) => Regex.Replace(content, "{@(\\w+)}", (match) => match.Groups[1].Value switch
            {
                "ApplicationState" => Guid.NewGuid().ToString(),
                _ => match.Value
            });
        }

        private async Task<IActionResult> AuthorizePost(CloudTable authorizationTokenTable, HttpRequestMessage requestMessage)
        {
            var token = await SetAuthorizationTokenAsync(authorizationTokenTable, new AuthorizationToken())
                .ConfigureAwait(false);

            var payload = await requestMessage.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            var fields = HttpUtility
                .ParseQueryString(payload)
                .ToDictionary();

            var queryParams = HttpUtility.ParseQueryString(string.Empty);

            queryParams["client_id"] = fields.GetValueOrDefault("applicationId")?.SingleOrDefault();
            queryParams["response_type"] = "Assertion";
            queryParams["state"] = token.Id.ToString();
            queryParams["scope"] = string.Join(' ', AuthorizationToken.Scopes);
            queryParams["redirect_uri"] = await GetCallbackUrl().ConfigureAwait(false);

            var uriBuilder = new UriBuilder(AuthUrl)
            {
                Query = queryParams.ToString()
            };

            return new RedirectResult(uriBuilder.ToString());
        }


        public static Task<string> GetCallbackUrl()
            => FunctionsEnvironment.GetFunctionUrlAsync(nameof(AuthorizationHandler) + nameof(Callback));

        [FunctionName(nameof(AuthorizationHandler) + nameof(Callback))]
        public async Task<ActionResult> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorize/callback")] HttpRequestMessage requestMessage,
            [Table(nameof(AuthorizationToken))] CloudTable authorizationTokenTable,
            ILogger log)
        {
            if (requestMessage is null)
                throw new ArgumentNullException(nameof(requestMessage));

            if (authorizationTokenTable is null)
                throw new ArgumentNullException(nameof(authorizationTokenTable));

            try
            {
                var queryParams = requestMessage.RequestUri.ParseQueryString();

                var code = queryParams.Get("code");

                if (string.IsNullOrEmpty(code))
                    return new BadRequestResult();

                var state = queryParams.Get("state");

                if (Guid.TryParse(state, out var sessionId))
                {
                    var authorizationToken = await GetAuthorizationTokenAsync(authorizationTokenTable, sessionId)
                        .ConfigureAwait(false);

                    if (authorizationToken is null)
                        return new BadRequestResult();
                    else
                        authorizationToken.IsPending = false;

                    var callbackUrl = await GetCallbackUrl()
                        .ConfigureAwait(false);

                    var form = new
                    {
                        client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                        client_assertion = "TooManySecrets",
                        grant_type = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                        assertion = queryParams.Get("code"),
                        redirect_uri = callbackUrl
                    };

                    var response = await TokenUrl
                        .WithHeaders(new MediaTypeWithQualityHeaderValue("application/json"))
                        .AllowAnyHttpStatus()
                        .PostUrlEncodedAsync(form)
                        .ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content
                            .ReadAsStringAsync()
                            .ConfigureAwait(false);

                        JsonConvert.PopulateObject(json, authorizationToken);

                        _ = await SetAuthorizationTokenAsync(authorizationTokenTable, authorizationToken)
                            .ConfigureAwait(false);

                        return new OkResult();
                    }
                    else
                    {
                        return new StatusCodeResult((int)response.StatusCode);
                    }
                }
                else
                {
                    return new BadRequestResult();
                }
            }
            catch (Exception exc)
            {
                log.LogError(exc, $"Processing authorization callback failed: {exc.Message}");

                throw;
            }
        }
    }
}
