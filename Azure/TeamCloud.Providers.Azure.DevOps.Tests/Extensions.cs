/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.Events;

namespace TeamCloud.Providers.Azure.DevOps
{
    internal static class Extensions
    {
        public static Task<T> ReadAsAsync<T>(this HttpContent httpContent, JsonSerializerSettings serializerSettings = null)
            => httpContent.ReadAsAsync<T>(JsonSerializer.CreateDefault(serializerSettings));

        public static async Task<T> ReadAsAsync<T>(this HttpContent httpContent, JsonSerializer serializer)
        {
            using var stream = await httpContent.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);

            return serializer.Deserialize<T>(jsonReader);
        }

        public static bool TryGetValue(this NameValueCollection collection, string key, out string value)
        {
            value = collection.AllKeys.Contains(key)
                ? collection.Get(key) : default;

            return value != default;
        }

        public static object SetValue(this JValue instance, object value)
        {
            if (instance is null)
                throw new System.ArgumentNullException(nameof(instance));

            return instance.Value = value;
        }

        public static EventFiringWebDriver WithEvents(this IWebDriver webDriver, ILogger log = null)
        {
            if (webDriver is null)
                throw new ArgumentNullException(nameof(webDriver));

            var eventWebDriver = webDriver as EventFiringWebDriver
                ?? new EventFiringWebDriver(webDriver);

            if (log != null)
            {
                eventWebDriver.Navigating += (object sender, WebDriverNavigationEventArgs e)
                    => log.LogInformation($"{GetBrowserInfo(sender as IWebDriver)} Navigating => {e.Url}");

                eventWebDriver.Navigated += (object sender, WebDriverNavigationEventArgs e)
                    => log.LogInformation($"{GetBrowserInfo(sender as IWebDriver)} Navigated => {e.Url}");

                eventWebDriver.ElementClicking += (object sender, WebElementEventArgs e)
                     => log.LogInformation($"{GetBrowserInfo(sender as IWebDriver)} ElementClicking => {GetElementName(e.Element)}");

                eventWebDriver.ElementClicked += (object sender, WebElementEventArgs e)
                    => log.LogInformation($"{GetBrowserInfo(sender as IWebDriver)} ElementClicked => {GetElementName(e.Element)}");

                eventWebDriver.ElementValueChanging += (object sender, WebElementValueEventArgs e)
                     => log.LogInformation($"{GetBrowserInfo(sender as IWebDriver)} ElementValueChanging => {GetElementName(e.Element)}");

                eventWebDriver.ElementValueChanged += (object sender, WebElementValueEventArgs e)
                     => log.LogInformation($"{GetBrowserInfo(sender as IWebDriver)} ElementValueChanged => {GetElementName(e.Element)}");
            }

            return eventWebDriver;

            string GetBrowserInfo(IWebDriver webDriver)
            {
                if (webDriver is null)
                    throw new ArgumentNullException(nameof(webDriver));

                if (webDriver is IWrapsDriver wrapsDriver)
                    return GetBrowserInfo(wrapsDriver.WrappedDriver);

                return $"{webDriver.GetType().Name} ({webDriver.Url})";
            }

            string GetElementName(IWebElement element)
            {
                try
                {
                    return element?.TagName;
                }
                catch (StaleElementReferenceException)
                {
                    return default;
                }
            }
        }

        public static T WrappedDriver<T>(this IWrapsDriver wrapsDriver)
            where T : class, IWebDriver
            => (T)wrapsDriver.WrappedDriver;
    }
}
