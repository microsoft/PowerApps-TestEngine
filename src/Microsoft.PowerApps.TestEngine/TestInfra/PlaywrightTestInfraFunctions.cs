// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
        private readonly IEnvironmentVariable _environmentVariable;
        private readonly IUserCertificateProvider _certificateProvider;

        public static string BrowserNotSupportedErrorMessage = "Browser not supported by Playwright, for more details check https://playwright.dev/dotnet/docs/browsers";
        private IPlaywright PlaywrightObject { get; set; }
        private IBrowser Browser { get; set; }
        private IBrowserContext BrowserContext { get; set; }
        public IPage Page { get; set; }
        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, ITestWebProvider testWebProvider, IEnvironmentVariable environmentVariable, IUserCertificateProvider certificateProvider)
        {
            _testState = testState;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
            _testWebProvider = testWebProvider;
            _environmentVariable = environmentVariable;
            _certificateProvider = certificateProvider;
        }

        // Constructor to aid with unit testing
        public PlaywrightTestInfraFunctions(ITestState testState, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem,
            IPlaywright playwrightObject = null, IBrowserContext browserContext = null, IPage page = null, ITestWebProvider testWebProvider = null, IEnvironmentVariable environmentVariable = null, IUserCertificateProvider certificateProvider = null) : this(testState, singleTestInstanceState, fileSystem, testWebProvider, environmentVariable, certificateProvider)
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

            if (!string.IsNullOrEmpty(testSettings.ExecutablePath))
            {
                launchOptions.ExecutablePath = testSettings.ExecutablePath;
                staticContext.ExecutablePath = testSettings.ExecutablePath;
            }

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

            if (userManager is IConfigurableUserManager configurableUserManager)
            {
                // Add file state as user manager may need access to file system
                configurableUserManager.Settings.Add("FileSystem", _fileSystem);
                // Add Evironment variable as provider may need additional settings
                configurableUserManager.Settings.Add("Environment", _environmentVariable);
                // Pass in current test state
                configurableUserManager.Settings.Add("TestState", _testState);
                configurableUserManager.Settings.Add("SingleTestState", _singleTestInstanceState);
                // Pass in certificate provider
                configurableUserManager.Settings.Add("UserCertificate", _certificateProvider);

                if (configurableUserManager.Settings.ContainsKey("LoadState")
                    && configurableUserManager.Settings["LoadState"] is Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string> loadState)
                {
                    var storageState = loadState.DynamicInvoke(_environmentVariable, _singleTestInstanceState, _testState, _fileSystem) as string;

                    // Optionally check if user manager wants to load a previous session state from storage
                    if (!string.IsNullOrEmpty(storageState))
                    {
                        _singleTestInstanceState.GetLogger().LogInformation("Loading storage stage");
                        contextOptions.StorageState = storageState;
                    }

                    // *** Storage State and Security context ***
                    //
                    // ** Why It Is Important: **
                    //
                    // ** Session Management: **
                    // Cookies are used to store session information, such as authentication tokens.
                    // Without the ability to store and retrieve cookies, the browser context cannot maintain the user's session, leading to authentication failures.
                    //
                    // ** Authentication State: **
                    // When a user logs in, the authentication tokens are often stored in cookies.
                    // These tokens are required for subsequent requests to authenticate the user.
                    // If cookies are not enabled, expired or related to sessions that are no longer valid, the browser context will not have access to these tokens or have tokens which are invalid.
                    // This resulting can result in errors like AADSTS50058.
                    //
                    // ** Example: **
                    // Lets look at an example of the impact of cookies and how it can generate Entra based login errors.
                    // The user initially logins in successfully using [Temporary Access Pass](https://learn.microsoft.com/entra/identity/authentication/howto-authentication-temporary-access-pass) with a lifetime of one hour.
                    //
                    // In this example we will later see AADSTS50058 error occuring when a silent sign-in request is sent, but no user is signed in after the Temporary Access Pass (TAP) with a lifetime has expired or had been revoked.
                    //
                    // Explaination:
                    // Test can receive error "AADSTS50058: A silent sign-in request was sent but no user is signed in."
                    // 
                    // The error occurs because the silent sign-in request is sent to the login.microsoftonline.com endpoint.
                    // Entra validates the request and determines the usable authentication methods and determine that the original TAP has expired
                    // This prompts the interactive sign in process again
                    //
                    // For deeper discussion
                    // 1. Start with [Microsoft Entra authentication documentation](https://learn.microsoft.com/entra/identity/authentication/)
                    // 1. Single Sign On and how it works review [Microsoft Entra seamless single sign-on: Technical deep dive](https://learn.microsoft.com/entra/identity/hybrid/connect/how-to-connect-sso-how-it-works)
                    // 2. [What authentication and verification methods are available in Microsoft Entra ID?](https://learn.microsoft.com/en-us/entra/identity/authentication/concept-authentication-methods)
                }
            }
            if (userManager.UseStaticContext)
            {
                //remove context directory if any present previously
                await RemoveContext(userManager);

                var location = userManager.ContextLocation;
                if (!Path.IsPathRooted(location))
                {
                    location = Path.Combine(_fileSystem.GetDefaultRootTestEngine(), location);
                }
                _fileSystem.CreateDirectory(location);

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

                    if (!_fileSystem.CanAccessFilePath(mock.ResponseDataFile) || !_fileSystem.FileExists(mock.ResponseDataFile))
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

        public async Task EndTestRunAsync(IUserManager userManager)
        {
            if (BrowserContext != null)
            {
                await Task.Delay(200);
                await BrowserContext.CloseAsync();
            }
            await RemoveContext(userManager);
        }

        public async Task RemoveContext(IUserManager userManager)
        {
            try
            {
                if (userManager.UseStaticContext)
                {
                    var location = userManager.ContextLocation;
                    if (!Path.IsPathRooted(location))
                    {
                        location = Path.Combine(_fileSystem.GetDefaultRootTestEngine(), location);
                    }
                    _fileSystem.DeleteDirectory(location);
                }
            }
            catch
            {
                _singleTestInstanceState.GetLogger().LogInformation("Missing context or error deleting context");
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
            if (!_fileSystem.CanAccessFilePath(screenshotFilePath))
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
