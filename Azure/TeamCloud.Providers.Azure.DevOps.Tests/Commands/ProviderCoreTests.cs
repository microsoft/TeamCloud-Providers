/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Commands.Core;
using TeamCloud.Model.Data;
using TeamCloud.Model.Data.Core;
using TeamCloud.Providers.Azure.DevOps.Conditional;
using TeamCloud.Providers.Azure.DevOps.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace TeamCloud.Providers.Azure.DevOps.Commands
{
    [Collection(nameof(ProviderContext))]
    public class ProviderCoreTests : ProviderCommandTests
    {
        public ProviderCoreTests(ProviderService providerService, ITestOutputHelper outputHelper)
            : this(providerService, XUnitLogger.Create<ProviderCoreTests>(outputHelper))
        { }

        internal ProviderCoreTests(ProviderService providerService, ILogger logger)
            : base(providerService, logger)
        { }

        [ConditionalFact(ConditionalFactPlatforms.Windows)]
        public async Task Register()
        {
            _ = await ProviderService
                .StartAsync()
                .ConfigureAwait(false);

            var user = await GetUserAsync()
                .ConfigureAwait(false);

            var command = new ProviderRegisterCommand(user, new ProviderConfiguration()
            {
                TeamCloudApplicationInsightsKey = Guid.Empty.ToString()
            });

            var commandResult = await SendCommandAsync(command, true)
                .ConfigureAwait(false);

            Assert.Equal(command.CommandId, commandResult.CommandId);
            Assert.Equal(CommandRuntimeStatus.Completed, commandResult.RuntimeStatus);
        }

        [ConditionalFact(ConditionalFactPlatforms.Windows)]
        public async Task Authorize()
        {
            await Register()
                .ConfigureAwait(false);

            var configuration = new ConfigurationBuilder()
                .AddUserSecrets(GetType().Assembly)
                .Build();

            var options = new ChromeOptions();

            if (!Debugger.IsAttached)
                options.AddArgument("--headless");

            var authorizationUrl = ProviderService.BaseUrl.AppendPathSegment("api/authorize");

            using var browser = new ChromeDriver(options).WithEvents(Logger);

            try
            {
                browser.Navigate().GoToUrl(authorizationUrl);

                new WebDriverWait(browser, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.TagName("body")));

                Logger.LogInformation($"{nameof(Authorize)}: {browser.Url}");

                foreach (var fieldId in new string[] { "organization", "client_id", "client_secret" })
                {
                    var field = browser.WrappedDriver<RemoteWebDriver>().FindElementById(fieldId) ?? throw new Exception($"Could not find element by id '{fieldId}'");

                    field.Clear(); // clear preset values
                    field.SendKeys(configuration.GetValue<string>(fieldId) ?? throw new Exception($"Could not find configuration secret '{fieldId}'"));
                }

                SubmitForm();

                while (browser.Url.StartsWith("https://login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var fieldType in new string[] { "email", "password" })
                    {
                        var field = browser.WrappedDriver<RemoteWebDriver>().FindElementsByXPath($"//input[@type='{fieldType}']").SingleOrDefault();

                        field?.Clear(); // clear preset values
                        field?.SendKeys(configuration.GetValue<string>(fieldType) ?? throw new Exception($"Could not find configuration secret '{fieldType}'"));
                    }

                    if (!SubmitForm()) break;
                }

                if (Uri.TryCreate(browser.Url, UriKind.Absolute, out var browserUrl))
                {
                    Assert.Equal(authorizationUrl, browserUrl.GetLeftPart(UriPartial.Path), true);
                    Assert.Contains("succeeded", browserUrl.ParseQueryString().AllKeys);
                }
                else
                {
                    throw new Exception($"Unexpected URL detected: {browser.Url}");
                }
            }
            finally
            {
                browser.Close();
            }

            bool SubmitForm()
            {
                while (true)
                {
                    try
                    {
                        var submit = new WebDriverWait(browser, TimeSpan.FromSeconds(10))
                            .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.XPath("//*[@type='submit']"))); ;

                        if (submit != null)
                        {
                            Logger.LogInformation("Submit");

                            submit.Click();

                            // wait for the final page to show up
                            // as we are dealing with an oauth dance
                            // there are multiple redirects to follow

                            new WebDriverWait(browser, TimeSpan.FromSeconds(10))
                                .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.TagName("body")));

                            return true;
                        }

                        return false;
                    }
                    catch (StaleElementReferenceException)
                    {
                        // swallow this exception and use the outer while to retry
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }
                }
            }
        }
    }
}
