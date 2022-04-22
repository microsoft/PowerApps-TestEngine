// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    /// <summary>
    /// Playwright implementation of the test infrastructure function
    /// </summary>
    public class PlaywrightTestInfraFunctions : ITestInfraFunctions
    {
        private readonly ITestState _testState;
        private readonly ISingleTestInstanceState _singleTestInstanceState;

        private IBrowser? Browser { get; set; }
        private IBrowserContext? BrowserContext { get; set; }
        private IPage? Page { get; set; }

        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState)
        {
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
        }

        public async Task SetupAsync()
        {
            var playwright = await Playwright.Playwright.CreateAsync();
            var launchOptions = new BrowserTypeLaunchOptions()
            {
                Headless = false
            };

            var browserConfig = _singleTestInstanceState.GetBrowserConfig();
            Browser = await playwright[browserConfig.Browser].LaunchAsync(launchOptions);

            var contextOptions = new BrowserNewContextOptions();

            if (!string.IsNullOrEmpty(_singleTestInstanceState.GetBrowserConfig().Device))
            {
                contextOptions = playwright.Devices[_singleTestInstanceState.GetBrowserConfig().Device];
            }

            if (_testState.GetTestSettings().RecordVideo)
            {
                contextOptions.RecordVideoDir = _singleTestInstanceState.GetTestResultsDirectory();
            }

            if (_singleTestInstanceState.GetBrowserConfig().ScreenWidth != null && _singleTestInstanceState.GetBrowserConfig().ScreenHeight != null)
            {
                contextOptions.ViewportSize = new ViewportSize()
                {
                    Width = _singleTestInstanceState.GetBrowserConfig().ScreenWidth.Value,
                    Height = _singleTestInstanceState.GetBrowserConfig().ScreenHeight.Value
                };
            }

            BrowserContext = await Browser.NewContextAsync(contextOptions);
        }

        public async Task GoToUrlAsync(string url)
        {
            if (Page == null)
            {
                Page = await BrowserContext.NewPageAsync();
            }

            // TODO: consider whether to make waiting for network idle state part of the function input
            await Page.GotoAsync(url, new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle });
        }
        public async Task EndTestRunAsync()
        {
            if (BrowserContext != null)
            {
                await BrowserContext.CloseAsync();
            }
        }
        public async Task ScreenshotAsync(string screenshotFilePath)
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions() { Path = $"{screenshotFilePath}" });

        }
        public async Task FillAsync(string selector, string value)
        {
            await Page.FillAsync(selector, value);
        }
        public async Task ClickAsync(string selector)
        {
            await Page.ClickAsync(selector);
        }
        public async Task AddScriptTagAsync(string scriptTag, string? frameName)
        {
            if (string.IsNullOrEmpty(frameName))
            {
                await Page.AddScriptTagAsync(new PageAddScriptTagOptions() { Path = scriptTag });
            }
            else
            {
                await Page.Frame(frameName).AddScriptTagAsync(new FrameAddScriptTagOptions() { Path = scriptTag });
            }
        }

        public async Task<T> RunJavascriptAsync<T>(string jsExpression, string? frameName)
        {
            if (string.IsNullOrEmpty(frameName))
            {
                return await Page.EvaluateAsync<T>(jsExpression);
            }
            else
            {
                return await Page.Frame(frameName).EvaluateAsync<T>(jsExpression);
            }
        }
    }
}
