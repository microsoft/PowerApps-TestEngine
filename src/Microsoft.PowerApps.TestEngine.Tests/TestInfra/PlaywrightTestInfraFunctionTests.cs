// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.TestInfra
{
    public class PlaywrightTestInfraFunctionTests
    {
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IPlaywright> MockPlaywrightObject;
        private Mock<IBrowserType> MockBrowserType;
        private Mock<IBrowser> MockBrowser;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<IPage> MockPage;
        private Mock<IResponse> MockResponse;
        private Mock<IRequest> MockRequest;
        private Mock<IRoute> MockRoute;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IFrame> MockIFrame;
        private Mock<IElementHandle> MockElementHandle;
        private Mock<ILogger> MockLogger;
        private Mock<ILoggerFactory> MockLoggerFactory;

        public PlaywrightTestInfraFunctionTests()
        {
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockPlaywrightObject = new Mock<IPlaywright>(MockBehavior.Strict);
            MockBrowserType = new Mock<IBrowserType>(MockBehavior.Strict);
            MockBrowser = new Mock<IBrowser>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockResponse = new Mock<IResponse>(MockBehavior.Strict);
            MockRequest = new Mock<IRequest>(MockBehavior.Strict);
            MockRoute = new Mock<IRoute>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockIFrame = new Mock<IFrame>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("Chromium", null, null, null)]
        [InlineData("Chromium", "Pixel 2", null, null)]
        [InlineData("Safari", "iPhone 8", 400, null)]
        [InlineData("Safari", "iPhone 8", 400, 800)]
        public async Task SetupAsyncTest(string browser, string device, int? screenWidth, int? screenHeight)
        {
            var browserConfig = new BrowserConfiguration()
            {
                Browser = browser,
                Device = device,
                ScreenHeight = screenHeight,
                ScreenWidth = screenWidth
            };

            var testSettings = new TestSettings()
            {
                RecordVideo = true,
                Timeout = 15
            };

            var testResultsDirectory = "C:\\TestResults";

            var devicesDictionary = new Dictionary<string, BrowserNewContextOptions>()
            {
                { "Pixel 2", new BrowserNewContextOptions() { UserAgent = "Pixel 2 User Agent "} },
                { "iPhone 8", new BrowserNewContextOptions() { UserAgent = "iPhone 8 User Agent "} }
            };

            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);
            MockPlaywrightObject.SetupGet(x => x[It.IsAny<string>()]).Returns(MockBrowserType.Object);
            MockPlaywrightObject.SetupGet(x => x.Devices).Returns(devicesDictionary);
            MockBrowserType.Setup(x => x.LaunchAsync(It.IsAny<BrowserTypeLaunchOptions>())).Returns(Task.FromResult(MockBrowser.Object));
            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(testResultsDirectory);
            MockBrowser.Setup(x => x.NewContextAsync(It.IsAny<BrowserNewContextOptions>())).Returns(Task.FromResult(MockBrowserContext.Object));
            LoggingTestHelper.SetupMock(MockLogger);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await playwrightTestInfraFunctions.SetupAsync();

            MockSingleTestInstanceState.Verify(x => x.GetBrowserConfig(), Times.Once());
            MockPlaywrightObject.Verify(x => x[browserConfig.Browser], Times.Once());
            MockBrowserType.Verify(x => x.LaunchAsync(It.Is<BrowserTypeLaunchOptions>(y => y.Headless == true && y.Timeout == testSettings.Timeout)), Times.Once());
            MockTestState.Verify(x => x.GetTestSettings(), Times.Once());

            if (browserConfig.Device != null)
            {
                MockPlaywrightObject.Verify(x => x.Devices, Times.Once());
            }
            MockSingleTestInstanceState.Verify(x => x.GetTestResultsDirectory(), Times.Once());

            var verifyBrowserContextOptions = (BrowserNewContextOptions options) =>
            {
                if (options.RecordVideoDir != testResultsDirectory)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(browserConfig.Device))
                {
                    var device = devicesDictionary[browserConfig.Device];
                    if (device.UserAgent != options.UserAgent)
                    {
                        return false;
                    }
                }

                if (browserConfig.ScreenWidth != null && browserConfig.ScreenHeight != null)
                {
                    if (browserConfig.ScreenWidth != options.ViewportSize.Width)
                    {
                        return false;
                    }
                    if (browserConfig.ScreenHeight != options.ViewportSize.Height)
                    {
                        return false;
                    }
                }
                else
                {
                    if (options.ViewportSize != null)
                    {
                        return false;
                    }
                }
                return true;
            };
            MockBrowser.Verify(x => x.NewContextAsync(It.Is<BrowserNewContextOptions>(y => verifyBrowserContextOptions(y))), Times.Once());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task SetupAsyncThrowsOnNullOrEmptyBrowserTest(string browser)
        {
            var browserConfig = new BrowserConfiguration()
            {
                Browser = browser
            };
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var testSettings = new TestSettings()
            {
                Headless = true,
                Timeout = 15
            };

            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, null);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.SetupAsync());
        }

        [Theory]
        [InlineData("Chrome")]
        [InlineData("Safari")]
        [InlineData("INVALID_BROWSER_NAME")]
        public async Task SetupAsyncThrowsOnInvalidBrowserTest(string browser)
        {
            var browserConfig = new BrowserConfiguration()
            {
                Browser = browser
            };
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var testSettings = new TestSettings()
            {
                Headless = true,
                Timeout = 15
            };

            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, null);

            // Act and Assert
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await playwrightTestInfraFunctions.SetupAsync());
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, PlaywrightTestInfraFunctions.BrowserNotSupportedErrorMessage, LogLevel.Error, Times.Once());
        }

        [Fact]
        public async Task SetupAsyncThrowsOnNullTestSettingsTest()
        {
            TestSettings testSettings = null;
            var browserConfig = new BrowserConfiguration()
            {
                Browser = "Chromium"
            };
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            MockTestState.Setup(x => x.GetTestSettings()).Returns(testSettings);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.SetupAsync());
        }

        [Fact]
        public async Task SetupAsyncThrowsOnNullBrowserConfigTest()
        {
            BrowserConfiguration browserConfig = null;
            MockSingleTestInstanceState.Setup(x => x.GetBrowserConfig()).Returns(browserConfig);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, MockPlaywrightObject.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.SetupAsync());
        }

        [Fact]
        public async Task EndTestRunSuccessTest()
        {
            MockBrowserContext.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.WaitForRequestFinishedAsync(It.IsAny<PageWaitForRequestFinishedOptions>())).Returns(Task.FromResult(MockRequest.Object));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object, page: MockPage.Object);

            await playwrightTestInfraFunctions.EndTestRunAsync();

            MockBrowserContext.Verify(x => x.CloseAsync(), Times.Once);
        }

        [Fact]
        public async Task SetupNetworkRequestMockAsyncTest()
        {
            var mock = new NetworkRequestMock()
            {
                RequestURL = "https://make.powerapps.com",
                ResponseDataFile = "response.json"
            };

            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                NetworkRequestMocks = new List<NetworkRequestMock> { mock },
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockPage.Setup(x => x.RouteAsync(mock.RequestURL, It.IsAny<Func<IRoute, Task>>(), It.IsAny<PageRouteOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await playwrightTestInfraFunctions.SetupNetworkRequestMockAsync();

            MockBrowserContext.Verify(x => x.NewPageAsync(), Times.Once);
            MockPage.Verify(x => x.RouteAsync(mock.RequestURL, It.IsAny<Func<IRoute, Task>>(), It.IsAny<PageRouteOptions>()), Times.Once);
            MockFileSystem.Verify(x => x.FileExists(mock.ResponseDataFile), Times.Once());
            MockFileSystem.Verify(x => x.IsValidFilePath(mock.ResponseDataFile), Times.Once());
        }

        [Fact]
        public async Task SetupNetworkRequestMockAsyncNullMockSkipTest()
        {

            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await playwrightTestInfraFunctions.SetupNetworkRequestMockAsync();
            MockBrowserContext.Verify(x => x.NewPageAsync(), Times.Never);
        }

        [Fact]
        public async Task SetupNetworkRequestMockAsyncThrowOnNullRequestURLTest()
        {
            var mock = new NetworkRequestMock()
            {
                RequestURL = "",
                ResponseDataFile = "response.json"
            };

            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                NetworkRequestMocks = new List<NetworkRequestMock> { mock },
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await playwrightTestInfraFunctions.SetupNetworkRequestMockAsync());
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString(), ex.Message);
        }

        [Fact]
        public async Task SetupNetworkRequestMockAsyncThrowOnInvalidFilePathTest()
        {
            var mock = new NetworkRequestMock()
            {
                RequestURL = "https://make.powerapps.com",
                ResponseDataFile = "response.json"
            };

            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                NetworkRequestMocks = new List<NetworkRequestMock> { mock },
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await playwrightTestInfraFunctions.SetupNetworkRequestMockAsync());
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionInvalidFilePath.ToString(), ex.Message);
        }

        [Fact]
        public async Task SetupNetworkRequestMockAsyncThrowOnEmptyFilePathTest()
        {
            var mock = new NetworkRequestMock()
            {
                RequestURL = "https://make.powerapps.com",
                ResponseDataFile = ""
            };

            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                NetworkRequestMocks = new List<NetworkRequestMock> { mock },
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await playwrightTestInfraFunctions.SetupNetworkRequestMockAsync());
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionInvalidFilePath.ToString(), ex.Message);
        }

        [Fact]
        public async Task GoToUrlTest()
        {
            var urlToVisit = "https://make.powerapps.com";

            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));
            MockResponse.SetupGet(x => x.Ok).Returns(true);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await playwrightTestInfraFunctions.GoToUrlAsync(urlToVisit);

            MockBrowserContext.Verify(x => x.NewPageAsync(), Times.Once);
            MockPage.Verify(x => x.GotoAsync(urlToVisit, It.IsAny<PageGotoOptions>()), Times.Once);

            var secondUrlToVisit = "https://powerapps.com";
            await playwrightTestInfraFunctions.GoToUrlAsync(secondUrlToVisit);
            MockBrowserContext.Verify(x => x.NewPageAsync(), Times.Once, "Should only create a new page once");
            MockPage.Verify(x => x.GotoAsync(secondUrlToVisit, It.IsAny<PageGotoOptions>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("www.microsoft.com")]
        [InlineData("file://c:/test.txt")]
        [InlineData("hi")]
        public async Task GoToUrlThrowsOnInvalidUrlTest(string url)
        {
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.GoToUrlAsync(url));
        }

        [Fact]
        public async Task GoToUrlThrowsOnUnsuccessfulResponseTest()
        {
            var urlToVisit = "https://make.powerapps.com";

            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockPage.Setup(x => x.GotoAsync(It.IsAny<string>(), It.IsAny<PageGotoOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));
            MockResponse.SetupGet(x => x.Ok).Returns(false);
            MockResponse.SetupGet(x => x.Status).Returns(404);
            LoggingTestHelper.SetupMock(MockLogger);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.GoToUrlAsync(urlToVisit));
            LoggingTestHelper.VerifyLogging(MockLogger, (message) => message.Contains(urlToVisit) && message.Contains("404"), LogLevel.Trace, Times.Once());
        }

        [Fact]
        public async Task PageFunctionsThrowOnNullPageTest()
        {
            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.ScreenshotAsync("1.jpg"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.FillAsync("[id=\"i0116\"]", "hello"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.ClickAsync("[id=\"i0116\"]"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.AddScriptTagAsync("script.js", "iframeName"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.RunJavascriptAsync<bool>("console.log(\"hi\")"));
        }

        [Fact]
        public async Task ScreenshotSuccessfulTest()
        {
            var screenshotFilePath = "1.jpg";

            MockPage.Setup(x => x.ScreenshotAsync(It.IsAny<PageScreenshotOptions>())).Returns(Task.FromResult(new byte[] { }));
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.ScreenshotAsync(screenshotFilePath);

            MockPage.Verify(x => x.ScreenshotAsync(It.Is<PageScreenshotOptions>((options) => options.Path == screenshotFilePath)), Times.Once());
            MockFileSystem.Verify(x => x.IsValidFilePath(screenshotFilePath), Times.Once());
        }

        [Fact]
        public async Task ScreenshotThrowsOnInvalidScreenshotFilePath()
        {
            var screenshotFilePath = "";
            MockPage.Setup(x => x.ScreenshotAsync(It.IsAny<PageScreenshotOptions>())).Returns(Task.FromResult(new byte[] { }));
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await playwrightTestInfraFunctions.ScreenshotAsync(screenshotFilePath));
            MockFileSystem.Verify(x => x.IsValidFilePath(screenshotFilePath), Times.Once());
            MockPage.Verify(x => x.ScreenshotAsync(It.Is<PageScreenshotOptions>((options) => options.Path == screenshotFilePath)), Times.Never());
        }

        [Fact]
        public async Task FillAsyncSuccessfulTest()
        {
            var selector = "input[type =\"email\"]";
            var value = "hello";

            MockPage.Setup(x => x.FillAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PageFillOptions?>())).Returns(Task.CompletedTask);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.FillAsync(selector, value);

            MockPage.Verify(x => x.FillAsync(selector, value, null), Times.Once());
        }

        [Fact]
        public async Task ClickAsyncSuccessfulTest()
        {
            var selector = "input[type =\"email\"]";

            MockPage.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<PageClickOptions?>())).Returns(Task.CompletedTask);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.ClickAsync(selector);

            MockPage.Verify(x => x.ClickAsync(selector, null), Times.Once());
        }

        [Fact]
        public async Task AddScriptTagSuccessfulTest()
        {
            var scriptTag = "test.js";
            string frameName = null;

            MockPage.Setup(x => x.AddScriptTagAsync(It.IsAny<PageAddScriptTagOptions>())).Returns(Task.FromResult(MockElementHandle.Object));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.AddScriptTagAsync(scriptTag, frameName);

            MockPage.Verify(x => x.AddScriptTagAsync(It.Is<PageAddScriptTagOptions>((options) => options.Path == scriptTag)), Times.Once());
        }

        [Fact]
        public async Task AddScriptTagToFrameSuccessfulTest()
        {
            var scriptTag = "test.js";
            var frameName = "publishedAppFrame";

            MockIFrame.Setup(x => x.AddScriptTagAsync(It.IsAny<FrameAddScriptTagOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            MockPage.Setup(x => x.Frame(It.IsAny<string>())).Returns(MockIFrame.Object);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            await playwrightTestInfraFunctions.AddScriptTagAsync(scriptTag, frameName);

            MockIFrame.Verify(x => x.AddScriptTagAsync(It.Is<FrameAddScriptTagOptions>((options) => options.Path == scriptTag)), Times.Once());
            MockPage.Verify(x => x.Frame(frameName), Times.Once());
        }

        [Fact]
        public async Task RunJavascriptSuccessfulTest()
        {
            var jsExpression = "console.log('hello')";
            var expectedResponse = "hello";

            LoggingTestHelper.SetupMock(MockLogger);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), It.IsAny<object?>())).Returns(Task.FromResult(expectedResponse));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, page: MockPage.Object);
            var result = await playwrightTestInfraFunctions.RunJavascriptAsync<string>(jsExpression);
            Assert.Equal(expectedResponse, result);

            LoggingTestHelper.VerifyLogging(MockLogger, (message) => message.Contains(jsExpression), LogLevel.Debug, Times.Once());
            MockPage.Verify(x => x.EvaluateAsync<string>(jsExpression, null), Times.Once());
        }

        [Fact]
        public async Task RouteNetworkRequestTest()
        {
            var requestHeader = new Dictionary<string, string>();
            requestHeader.Add("x-ms-request-method", "PATCH");
            var requestBody = "request body";
            var requestMethod = "POST";

            var mock = new NetworkRequestMock()
            {
                RequestURL = "https://make.powerapps.com",
                Method = requestMethod,
                Headers = requestHeader,
                RequestBodyFile = "request.json",
                ResponseDataFile = "response.json"
            };

            MockFileSystem.Setup(x => x.ReadAllText(mock.RequestBodyFile)).Returns(requestBody);
            MockRoute.Setup(x => x.Request).Returns(MockRequest.Object);
            MockRequest.Setup(x => x.Method).Returns(requestMethod);
            MockRequest.Setup(x => x.PostData).Returns(requestBody);
            MockRequest.Setup(x => x.HeaderValueAsync("x-ms-request-method")).Returns(Task.FromResult<string>("PATCH"));
            MockRoute.Setup(x => x.FulfillAsync(It.IsAny<RouteFulfillOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));
            MockRoute.Setup(x => x.ContinueAsync(It.IsAny<RouteContinueOptions>())).Returns(Task.FromResult<IResponse?>(MockResponse.Object));

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
                MockFileSystem.Object, browserContext: MockBrowserContext.Object);

            // Test fulfilling route's request with given response
            await playwrightTestInfraFunctions.RouteNetworkRequest(MockRoute.Object, mock);
            MockRoute.Verify(x => x.FulfillAsync(It.Is<RouteFulfillOptions>((option) => option.Path == mock.ResponseDataFile)), Times.Once);

            // Test continuing route's request without overrides
            MockRequest.Setup(x => x.HeaderValueAsync("x-ms-request-method")).Returns(Task.FromResult<string>("POST"));
            await playwrightTestInfraFunctions.RouteNetworkRequest(MockRoute.Object, mock);
            MockRoute.Verify(x => x.ContinueAsync(It.IsAny<RouteContinueOptions>()), Times.Once);
        }

        [Fact]
        public async Task HandleUserPasswordScreen()
        {
            string testSelector = "input:has-text('Password')";
            string testTextEntry = "*****";
            string desiredUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var mockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.Locator(It.IsAny<string>(), null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.FillAsync(testSelector, testTextEntry, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync("input[type=\"submit\"]", null)).Returns(Task.CompletedTask);
            // Assume ask already logged in
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.IsAny<PageWaitForSelectorOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            // Simulate Click to stay signed in 
            MockPage.Setup(x => x.ClickAsync("[id=\"idBtn_Back\"]", null)).Returns(Task.CompletedTask);
            // Wait until login is complete and redirect to desired page
            MockPage.Setup(x => x.WaitForURLAsync(desiredUrl, null)).Returns(Task.CompletedTask);

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
               MockFileSystem.Object, browserContext: MockBrowserContext.Object, page: MockPage.Object);

            await playwrightTestInfraFunctions.HandleUserPasswordScreen(testSelector, testTextEntry, desiredUrl);

            MockPage.Verify(x => x.Locator(It.Is<string>(v => v.Equals(testSelector)), null));
            MockPage.Verify(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.Is<PageWaitForSelectorOptions>(v => v.Timeout >= 8000)));
        }

        [Fact]
        public async Task HandleUserPasswordScreenErrorEntry()
        {
            string testSelector = "input:has-text('Password')";
            string testTextEntry = "*****";
            string desiredUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var mockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.Locator(It.IsAny<string>(), null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.FillAsync(testSelector, testTextEntry, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync("input[type=\"submit\"]", null)).Returns(Task.CompletedTask);
            // Not ask to sign in as selector not found
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.IsAny<PageWaitForSelectorOptions>())).Throws(new TimeoutException());
            // Simulate error response for password error
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"passwordError\"]", It.IsAny<PageWaitForSelectorOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            // Throw exception as not make it to desired url
            MockPage.Setup(x => x.WaitForURLAsync(desiredUrl, null)).Throws(new TimeoutException());

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
               MockFileSystem.Object, browserContext: MockBrowserContext.Object, page: MockPage.Object);

            // scenario where password error or missing
            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await playwrightTestInfraFunctions.HandleUserPasswordScreen(testSelector, testTextEntry, desiredUrl));

            MockPage.Verify(x => x.Locator(It.Is<string>(v => v.Equals(testSelector)), null));
            MockPage.Verify(x => x.WaitForSelectorAsync("[id=\"passwordError\"]", It.Is<PageWaitForSelectorOptions>(v => v.Timeout >= 2000)));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString(), ex.Message);
        }

        [Fact]
        public async Task HandleUserPasswordScreenUnknownError()
        {
            string testSelector = "input:has-text('Password')";
            string testTextEntry = "*****";
            string desiredUrl = "https://make.powerapps.com";

            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var mockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.Locator(It.IsAny<string>(), null)).Returns(mockLocator.Object);
            mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.FillAsync(testSelector, testTextEntry, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync("input[type=\"submit\"]", null)).Returns(Task.CompletedTask);
            // Not ask to sign in as selector not found
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", null)).Throws(new TimeoutException());
            // Also not able to find password error, must be some other error
            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"passwordError\"]", It.IsAny<PageWaitForSelectorOptions>())).Throws(new TimeoutException());
            // Throw exception as not make it to desired url
            MockPage.Setup(x => x.WaitForURLAsync(desiredUrl, null)).Throws(new TimeoutException());

            var playwrightTestInfraFunctions = new PlaywrightTestInfraFunctions(MockTestState.Object, MockSingleTestInstanceState.Object,
               MockFileSystem.Object, browserContext: MockBrowserContext.Object, page: MockPage.Object);

            await Assert.ThrowsAsync<TimeoutException>(async () => await playwrightTestInfraFunctions.HandleUserPasswordScreen(testSelector, testTextEntry, desiredUrl));

            MockPage.Verify(x => x.Locator(It.Is<string>(v => v.Equals(testSelector)), null));
        }

    }
}
