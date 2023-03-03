// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;

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

        private IPlaywright PlaywrightObject { get; set; }
        private IBrowser Browser { get; set; }
        private IBrowserContext BrowserContext { get; set; }
        private IPage Page { get; set; }

        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem)
        {
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
        }

        // Constructor to aid with unit testing
        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem,
            IPlaywright playwrightObject = null, IBrowserContext browserContext = null, IPage page = null) : this(testState, singleTestInstanceState, fileSystem)
        {
            PlaywrightObject = playwrightObject;
            Page = page;
            BrowserContext = browserContext;
        }

        public async Task SetupAsync()
        {

            var browserConfig = _singleTestInstanceState.GetBrowserConfig();

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

            var browser = PlaywrightObject[browserConfig.Browser];
            if (browser == null)
            {
                _singleTestInstanceState.GetLogger().LogError("Browser not supported by Playwright, for more details check https://playwright.dev/dotnet/docs/browsers");
                throw new InvalidOperationException("Browser not supported.");
            }

            Browser = await browser.LaunchAsync(launchOptions);
            _singleTestInstanceState.GetLogger().LogInformation("Browser setup finished");

            var contextOptions = new BrowserNewContextOptions();

            if (!string.IsNullOrEmpty(browserConfig.Device))
            {
                contextOptions = PlaywrightObject.Devices[browserConfig.Device];
            }

            if (testSettings.RecordVideo)
            {
                contextOptions.RecordVideoDir = _singleTestInstanceState.GetTestResultsDirectory();
            }

            if (browserConfig.ScreenWidth != null && browserConfig.ScreenHeight != null)
            {
                contextOptions.ViewportSize = new ViewportSize()
                {
                    Width = browserConfig.ScreenWidth.Value,
                    Height = browserConfig.ScreenHeight.Value
                };
            }

            BrowserContext = await Browser.NewContextAsync(contextOptions);
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

                if (string.IsNullOrEmpty(mock.RequestURL))
                {
                    _singleTestInstanceState.GetLogger().LogError("RequestURL cannot be null");
                    throw new InvalidOperationException();
                }

                if (string.IsNullOrEmpty(mock.ResponseDataFile) || !_fileSystem.IsValidFilePath(mock.ResponseDataFile))
                {
                    _singleTestInstanceState.GetLogger().LogError("ResponseDataFile is invalid or missing");
                    throw new InvalidOperationException();
                }

                await Page.RouteAsync(mock.RequestURL, async route => await RouteNetworkRequest(route, mock));
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

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                _singleTestInstanceState.GetLogger().LogError("Url must be http/https");
                throw new InvalidOperationException();
            }

            if (Page == null)
            {
                Page = await BrowserContext.NewPageAsync();
            }

            // TODO: consider whether to make waiting for network idle state part of the function input
            var response = await Page.GotoAsync(url, new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle });

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

            if (!jsExpression.Equals(PowerAppFunctions.CheckPowerAppsTestEngineObject)) {
                _singleTestInstanceState.GetLogger().LogDebug("Run Javascript: " + jsExpression);
            }            

            return await Page.EvaluateAsync<T>(jsExpression);
        }

        // Justification: Limited ability to run unit tests for 
        // Playwright actions on the sign-in page
        [ExcludeFromCodeCoverage]
        public async Task HandleUserEmailScreen(string selector, string value)
        {
            ValidatePage();
            await Page.Locator(selector).WaitForAsync();
            await Page.TypeAsync(selector, value, new PageTypeOptions { Delay = 50 });
            await Page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
        }

        // Justification: Limited ability to run unit tests for 
        // Playwright actions on the sign-in page
        [ExcludeFromCodeCoverage]
        public async Task HandleUserPasswordScreen(string selector, string value, string desiredUrl)
        {
            var logger = _singleTestInstanceState.GetLogger();

            // Setting options fot the RunAndWaitForNavigationAsync function
            PageRunAndWaitForNavigationOptions options = new PageRunAndWaitForNavigationOptions();

            // URL that should be redirected to
            options.UrlString = desiredUrl;

            ValidatePage();

            try
            {
                // Only continue if redirected to the correct page
                await Page.RunAndWaitForNavigationAsync(async () =>
                {
                    // Find the password box
                    await Page.Locator(selector).WaitForAsync();

                    // Fill in the password
                    await Page.FillAsync(selector, value);

                    // Submit password form
                    await this.ClickAsync("input[type=\"submit\"]");

                    PageWaitForSelectorOptions selectorOptions = new PageWaitForSelectorOptions();
                    selectorOptions.Timeout = 8000;

                    // For instances where there is a 'Stay signed in?' dialogue box
                    try
                    {
                        logger.LogDebug("Checking if asked to stay signed in.");

                        // Check if we received a 'Stay signed in?' box?
                        await Page.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", selectorOptions);
                        logger.LogDebug("Was asked to 'stay signed in'.");

                        // Click to stay signed in
                        await Page.ClickAsync("[id=\"idBtn_Back\"]");
                    }
                    // If there is no 'Stay signed in?' box, an exception will throw; just catch and continue
                    catch (Exception ssiException)
                    {
                        logger.LogDebug("Exception encountered: " + ssiException.ToString());

                        // Keep record if passwordError was encountered
                        bool hasPasswordError = false;

                        try
                        {
                            selectorOptions.Timeout = 2000;

                            // Check if we received a password error
                            await Page.WaitForSelectorAsync("[id=\"passwordError\"]", selectorOptions);
                            hasPasswordError = true;
                        }
                        catch (Exception peException)
                        {
                            logger.LogDebug("Exception encountered: " + peException.ToString());
                        }

                        // If encountered password error, exit program
                        if (hasPasswordError)
                        {
                            logger.LogError("Incorrect password entered. Make sure you are using the correct credentials.");
                            throw new InvalidOperationException();
                        }
                        // If not, continue
                        else
                        {
                            logger.LogDebug("Did not encounter an invalid password error.");
                        }

                        logger.LogDebug("Was not asked to 'stay signed in'.");
                    }

                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }, options);
            }
            catch (TimeoutException)
            {
                logger.LogError("Timed out during login attempt. In order to determine why, it may be beneficial to view the output recording. Make sure that your login credentials are correct.");
                throw new TimeoutException();
            }

            logger.LogDebug("Logged in successfully.");
        }
    }
}
