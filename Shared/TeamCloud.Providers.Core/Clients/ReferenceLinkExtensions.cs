using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using TeamCloud.Azure;
using TeamCloud.Model.Common;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Core.Clients
{
    public static class ReferenceLinkExtensions
    {
        public static async Task<HttpResponseMessage> GetAsync(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, Func<string, string> tokenCallback = null)
        {
            if (referenceLink is null)
                throw new ArgumentNullException(nameof(referenceLink));

            if (!Uri.TryCreate(referenceLink.HRef, UriKind.Absolute, out _))
                throw new ArgumentException($"{nameof(referenceLink)} must provide a valid URL.", nameof(referenceLink));

            if (azureSessionService is null)
                throw new ArgumentNullException(nameof(azureSessionService));

            var token = await azureSessionService
                .AcquireTokenAsync()
                .ConfigureAwait(false);

            return await referenceLink.ToString(tokenCallback)
                .WithOAuthBearerToken(token)
                .GetAsync()
                .ConfigureAwait(false);
        }

        public static async Task<T> GetAsync<T>(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, Func<string, string> tokenCallback = null)
            where T : class, new()
        {
            if (typeof(T) == typeof(JObject))
            {
                var response = await GetAsync(referenceLink, azureSessionService, tokenCallback)
                    .ConfigureAwait(false);

                return await response.Content
                    .ReadAsAsync<T>()
                    .ConfigureAwait(false);
            }
            else
            {
                var json = await GetAsync<JObject>(referenceLink, azureSessionService, tokenCallback)
                    .ConfigureAwait(false);

                return json.SelectToken("data")?.ToObject<T>();
            }
        }

        public static async Task<T> PostAsync<T>(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, T payload, Func<string, string> tokenCallback = null)
            where T : class, IIdentifiable, new()
        {
            if (referenceLink is null)
                throw new ArgumentNullException(nameof(referenceLink));

            if (!Uri.TryCreate(referenceLink.HRef, UriKind.Absolute, out _))
                throw new ArgumentException($"{nameof(referenceLink)} must provide a valid URL.", nameof(referenceLink));

            if (azureSessionService is null)
                throw new ArgumentNullException(nameof(azureSessionService));

            if (payload is null)
                throw new ArgumentNullException(nameof(payload));

            var token = await azureSessionService
                .AcquireTokenAsync()
                .ConfigureAwait(false);

            var response = await referenceLink.ToString(tokenCallback)
                .WithOAuthBearerToken(token)
                .PostJsonAsync(payload)
                .ConfigureAwait(false);

            var json = await response.Content
                .ReadAsAsync<JObject>()
                .ConfigureAwait(false);

            return json.SelectToken("data")?.ToObject<T>();
        }

        public static async Task<T> PutAsync<T>(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, T payload, Func<string, string> tokenCallback = null)
            where T : class, IIdentifiable, new()
        {
            if (referenceLink is null)
                throw new ArgumentNullException(nameof(referenceLink));

            if (!Uri.TryCreate(referenceLink.HRef, UriKind.Absolute, out _))
                throw new ArgumentException($"{nameof(referenceLink)} must provide a valid URL.", nameof(referenceLink));

            if (azureSessionService is null)
                throw new ArgumentNullException(nameof(azureSessionService));

            if (payload is null)
                throw new ArgumentNullException(nameof(payload));

            var token = await azureSessionService
                .AcquireTokenAsync()
                .ConfigureAwait(false);

            var response = await referenceLink.ToString(tokenCallback)
                .AppendPathSegment(payload.Id)
                .WithOAuthBearerToken(token)
                .PutJsonAsync(payload)
                .ConfigureAwait(false);

            var json = await response.Content
                .ReadAsAsync<JObject>()
                .ConfigureAwait(false);

            return json.SelectToken("data")?.ToObject<T>();
        }

        public static async Task<T> SetAsync<T>(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, T payload, Func<string, string> tokenCallback = null)
            where T : class, IIdentifiable, new()
        {
            if (referenceLink is null)
                throw new ArgumentNullException(nameof(referenceLink));

            try
            {
                return await referenceLink
                    .PutAsync(azureSessionService, payload, tokenCallback)
                    .ConfigureAwait(false);
            }
            catch (FlurlHttpException exc) when (exc.Call.HttpStatus == System.Net.HttpStatusCode.NotFound)
            {
                return await referenceLink
                    .PostAsync(azureSessionService, payload, tokenCallback)
                    .ConfigureAwait(false);
            }
        }

        public static Task<T> DeleteAsync<T>(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, T payload, Func<string, string> tokenCallback = null)
            where T : class, IIdentifiable, new()
            => DeleteAsync<T>(referenceLink, azureSessionService, payload?.Id, tokenCallback);

        public static async Task<T> DeleteAsync<T>(this ReferenceLink referenceLink, IAzureSessionService azureSessionService, string identifier, Func<string, string> tokenCallback = null)
            where T : class, IIdentifiable, new()
        {
            if (referenceLink is null)
                throw new ArgumentNullException(nameof(referenceLink));

            if (!Uri.TryCreate(referenceLink.HRef, UriKind.Absolute, out _))
                throw new ArgumentException($"{nameof(referenceLink)} must provide a valid URL.", nameof(referenceLink));

            if (azureSessionService is null)
                throw new ArgumentNullException(nameof(azureSessionService));

            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            var token = await azureSessionService
                .AcquireTokenAsync()
                .ConfigureAwait(false);

            var response = await referenceLink.ToString(tokenCallback)
                .AppendPathSegment(identifier)
                .WithOAuthBearerToken(token)
                .DeleteAsync()
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NoContent)
                return default;

            var json = await response.Content
                .ReadAsAsync<JObject>()
                .ConfigureAwait(false);

            return json.SelectToken("data")?.ToObject<T>();
        }
    }
}
