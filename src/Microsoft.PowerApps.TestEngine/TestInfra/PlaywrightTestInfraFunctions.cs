// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
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

        private IPlaywright? PlaywrightObject { get; set; }
        private IBrowser? Browser { get; set; }
        private IBrowserContext? BrowserContext { get; set; }
        private IPage? Page { get; set; }

        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem)
        {
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
        }

        // Constructor to aid with unit testing
        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem,
            IPlaywright? playwrightObject = null, IBrowserContext? browserContext = null, IPage? page = null) : this(testState, singleTestInstanceState, fileSystem)
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
                throw new InvalidOperationException("Browser config cannot be null");
            }

            if (string.IsNullOrEmpty(browserConfig.Browser))
            {
                throw new InvalidOperationException("Browser cannot be null");
            }

            if (PlaywrightObject == null)
            {
                PlaywrightObject = await Playwright.Playwright.CreateAsync();
            }

            var testSettings = _testState.GetTestSettings();

            if (testSettings == null)
            {
                throw new InvalidOperationException("Test settings cannot be null");
            }

            var launchOptions = new BrowserTypeLaunchOptions()
            {
                Headless = false,
                Timeout = testSettings.Timeout
            };


            Browser = await PlaywrightObject[browserConfig.Browser].LaunchAsync(launchOptions);

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
        }

        public async Task GoToUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException("Url cannot be null or empty");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new InvalidOperationException("Url is invalid");
            }

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            {
                throw new InvalidOperationException("Url must be http/https");
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
            if (response != null && !response.Ok )
            {
                _singleTestInstanceState.GetLogger().LogError($"Error navigating to page: {url}, response is: {response?.Status}");
                throw new InvalidOperationException("Go to url failed");
            }
        }

        public async Task EndTestRunAsync()
        {
            if (BrowserContext != null)
            {
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

        public async Task AddScriptTagAsync(string scriptTag, string? frameName)
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

            return await Page.EvaluateAsync<T>(jsExpression);
        }
    }
}
