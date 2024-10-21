// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    /// <summary>
    /// Playwright implementation of the test infrastructure function
    /// </summary>
    public class PlaywrightTestInfraFunctions : ITestInfraFunctions
    {
        private readonly ITestState _testState;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly IFileSystem _fileSystem;
        private readonly ITestWebProvider _testWebProvider;

        public static string BrowserNotSupportedErrorMessage = "Browser not supported by Playwright, for more details check https://playwright.dev/dotnet/docs/browsers";
        private IPlaywright PlaywrightObject { get; set; }
        private IBrowser Browser { get; set; }
        private IBrowserContext BrowserContext { get; set; }
        public IPage Page { get; set; }
        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, ITestWebProvider testWebProvider)
        {
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
            _testWebProvider = testWebProvider;
        }

        // Constructor to aid with unit testing
        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem,
            IPlaywright playwrightObject = null, IBrowserContext browserContext = null, IPage page = null, ITestWebProvider testWebProvider = null) : this(testState, singleTestInstanceState, fileSystem, testWebProvider)
        {
            PlaywrightObject = playwrightObject;
            Page = page;
            BrowserContext = browserContext;
        }

        public IBrowserContext GetContext()
        {
            return BrowserContext;
        }

        public async Task SetupAsync(IUserManager userManager)
        {

            var browserConfig = _singleTestInstanceState.GetBrowserConfig();

            var staticContext = new BrowserTypeLaunchPersistentContextOptions();

            if (browserConfig == null)
            {
                _singleTestInstanceState.GetLogger().LogError("Browser config cannot be null");
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(browserConfig.Browser))
            {
                _singleTestInstanceState.GetLogger().LogError("Browser cannot be null");
                throw new InvalidOperationException();
            }

            if (PlaywrightObject == null)
            {
                PlaywrightObject = await Playwright.Playwright.CreateAsync();
            }

            var testSettings = _testState.GetTestSettings();

            if (testSettings == null)
            {
                _singleTestInstanceState.GetLogger().LogError("Test settings cannot be null.");
                throw new InvalidOperationException();
            }

            var launchOptions = new BrowserTypeLaunchOptions()
            {
                Headless = testSettings.Headless,
                Timeout = testSettings.Timeout
            };

            staticContext.Headless = launchOptions.Headless;
            staticContext.Timeout = launchOptions.Timeout;

            var browser = PlaywrightObject[browserConfig.Browser];
            if (browser == null)
            {
                _singleTestInstanceState.GetLogger().LogError("Browser not supported by Playwright, for more details check https://playwright.dev/dotnet/docs/browsers");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings.ToString());
            }

            if (!userManager.UseStaticContext)
            {
                // Check if a channel has been specified
                if (!string.IsNullOrEmpty(browserConfig.Channel))
                {
                    launchOptions.Channel = browserConfig.Channel;
                }

                Browser = await browser.LaunchAsync(launchOptions);
                _singleTestInstanceState.GetLogger().LogInformation("Browser setup finished");
            }

            var contextOptions = new BrowserNewContextOptions();

            // Use local when start browser
            contextOptions.Locale = testSettings.Locale;
            staticContext.Locale = contextOptions.Locale;


            if (!string.IsNullOrEmpty(browserConfig.Device))
            {
                contextOptions = PlaywrightObject.Devices[browserConfig.Device];
            }

            if (testSettings.RecordVideo)
            {
                contextOptions.RecordVideoDir = _singleTestInstanceState.GetTestResultsDirectory();
                staticContext.RecordVideoDir = contextOptions.RecordVideoDir;
            }

            if (browserConfig.ScreenWidth != null && browserConfig.ScreenHeight != null)
            {
                contextOptions.ViewportSize = new ViewportSize()
                {
                    Width = browserConfig.ScreenWidth.Value,
                    Height = browserConfig.ScreenHeight.Value
                };
                staticContext.RecordVideoSize = new RecordVideoSize()
                {
                    Width = browserConfig.ScreenWidth.Value,
                    Height = browserConfig.ScreenHeight.Value,
                };
            }

            if (testSettings.ExtensionModules != null && testSettings.ExtensionModules.Enable)
            {
                foreach (var module in _testState.GetTestEngineModules())
                {
                    module.ExtendBrowserContextOptions(contextOptions, testSettings);
                }
            }

            if (userManager.UseStaticContext)
            {
                _fileSystem.CreateDirectory(userManager.Location);
                var location = userManager.Location;
                if (!Path.IsPathRooted(location))
                {
                    location = Path.Combine(Directory.GetCurrentDirectory(), location);
                }
                _singleTestInstanceState.GetLogger().LogInformation($"Using static context in '{location}' using {userManager.Name}");

                // Check if a channel has been specified
                if (!string.IsNullOrEmpty(browserConfig.Channel))
                {
                    staticContext.Channel = browserConfig.Channel;
                }
                BrowserContext = await browser.LaunchPersistentContextAsync(location, staticContext);
            }
            else
            {
                BrowserContext = await Browser.NewContextAsync(contextOptions);
            }

            _singleTestInstanceState.GetLogger().LogInformation("Browser context created");
        }

        public async Task SetupNetworkRequestMockAsync()
        {

            var mocks = _singleTestInstanceState.GetTestSuiteDefinition().NetworkRequestMocks;

            if (mocks == null || mocks.Count == 0)
            {
                return;
            }

            if (Page == null)
            {
                Page = await BrowserContext.NewPageAsync();
            }

            foreach (var mock in mocks)
            {
                if (mock.IsExtension)
                {
                    foreach (var module in _testState.GetTestEngineModules())
                    {
                        await module.RegisterNetworkRoute(_testState, _singleTestInstanceState, _fileSystem, Page, mock);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(mock.RequestURL))
                    {
                        _singleTestInstanceState.GetLogger().LogError("RequestURL cannot be null");
                        throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString());
                    }

                    if (string.IsNullOrEmpty(mock.RequestURL))
                    {
                        _singleTestInstanceState.GetLogger().LogError("RequestURL cannot be null");
                        throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString());
                    }

                    if (!_fileSystem.IsValidFilePath(mock.ResponseDataFile) || !_fileSystem.FileExists(mock.ResponseDataFile))
                    {
                        _singleTestInstanceState.GetLogger().LogError("ResponseDataFile is invalid or missing");
                        throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionInvalidFilePath.ToString());
                    }

                    await Page.RouteAsync(mock.RequestURL, async route => await RouteNetworkRequest(route, mock));
                }
            }
        }

        public async Task RouteNetworkRequest(IRoute route, NetworkRequestMock mock)
        {
            // For optional properties of NetworkRequestMock, if the property is not specified, 
            // the routing applies to all. Ex: If Method is null, we mock response whatever the method is.
            bool notMatch = false;

            if (!string.IsNullOrEmpty(mock.Method))
            {
                notMatch = !string.Equals(mock.Method, route.Request.Method);
            }

            if (!string.IsNullOrEmpty(mock.RequestBodyFile))
            {
                notMatch = notMatch || !string.Equals(route.Request.PostData, _fileSystem.ReadAllText(mock.RequestBodyFile));
            }

            if (mock.Headers != null && mock.Headers.Count != 0)
            {
                foreach (var header in mock.Headers)
                {
                    var requestHeaderValue = await route.Request.HeaderValueAsync(header.Key);
                    notMatch = notMatch || !string.Equals(header.Value, requestHeaderValue);
                }
            }

            if (!notMatch)
            {
                await route.FulfillAsync(new RouteFulfillOptions { Path = mock.ResponseDataFile });
            }
            else
            {
                await route.ContinueAsync();
            }
        }

        public async Task GoToUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _singleTestInstanceState.GetLogger().LogError("Url cannot be null or empty");
                throw new InvalidOperationException();
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                _singleTestInstanceState.GetLogger().LogError("Url is invalid");
                throw new InvalidOperationException();
            }

            if ((uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                if (url != "about:blank")
                {
                    _singleTestInstanceState.GetLogger().LogError("Url must be http/https");
                    throw new InvalidOperationException();
                }
            }

            if (Page == null)
            {
                Page = await BrowserContext.NewPageAsync();
            }

            var response = await Page.GotoAsync(url);

            // The response might be null because "The method either throws an error or returns a main resource response.
            // The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null."
            //(From playwright https://playwright.dev/dotnet/docs/api/class-page#page-goto)
            if (response != null && !response.Ok)
            {
                _singleTestInstanceState.GetLogger().LogTrace($"Page is {url}, response is {response?.Status}");
                _singleTestInstanceState.GetLogger().LogError($"Error navigating to page.");
                throw new InvalidOperationException();
            }
        }

        public async Task EndTestRunAsync()
        {
            if (BrowserContext != null)
            {
                await Task.Delay(200);
                await BrowserContext.CloseAsync();
            }
        }

        public async Task DisposeAsync()
        {
            if (BrowserContext != null)
            {
                await BrowserContext.DisposeAsync();
                BrowserContext = null;
            }
            if (PlaywrightObject != null)
            {
                PlaywrightObject.Dispose();
                PlaywrightObject = null;
            }
        }

        private void ValidatePage()
        {
            if (Page == null)
            {
                throw new InvalidOperationException("Page is null, make sure to call GoToUrlAsync first");
            }
        }

        public async Task ScreenshotAsync(string screenshotFilePath)
        {
            ValidatePage();
            if (!_fileSystem.IsValidFilePath(screenshotFilePath))
            {
                throw new InvalidOperationException("screenshotFilePath must be provided");
            }

            await Page.ScreenshotAsync(new PageScreenshotOptions() { Path = $"{screenshotFilePath}" });
        }

        public async Task FillAsync(string selector, string value)
        {
            ValidatePage();
            await Page.FillAsync(selector, value);
        }

        public async Task ClickAsync(string selector)
        {
            ValidatePage();
            await Page.ClickAsync(selector);
        }

        public async Task AddScriptTagAsync(string scriptTag, string frameName)
        {
            ValidatePage();
            if (string.IsNullOrEmpty(frameName))
            {
                await Page.AddScriptTagAsync(new PageAddScriptTagOptions() { Path = scriptTag });
            }
            else
            {
                await Page.Frame(frameName).AddScriptTagAsync(new FrameAddScriptTagOptions() { Path = scriptTag });
            }
        }

        public async Task<T> RunJavascriptAsync<T>(string jsExpression)
        {
            ValidatePage();

            if (!jsExpression.Equals(_testWebProvider.CheckTestEngineObject))
            {
                _singleTestInstanceState.GetLogger().LogDebug("Run Javascript: " + jsExpression);
            }

            return await Page.EvaluateAsync<T>(jsExpression);
        }

        public async Task AddScriptContentAsync(string content)
        {
            ValidatePage();

            await Page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = content });
        }
    }
}
