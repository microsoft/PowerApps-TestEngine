// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Users;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests
{
    public class SingleTestRunnerTests
    {

        private Mock<ITestReporter> MockTestReporter;
        private Mock<IPowerFxEngine> MockPowerFxEngine;
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<IUserManager> MockUserManager;
        private Mock<ILoggerProvider> MockLoggerProvider;
        private Mock<ISingleTestInstanceState> MockTestState;
        private Mock<IUrlMapper> MockUrlMapper;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ILogger> MockLogger;
        private Mock<ITestLogger> MockTestLogger;

        public SingleTestRunnerTests()
        {
            MockTestReporter = new Mock<ITestReporter>(MockBehavior.Strict);
            MockPowerFxEngine = new Mock<IPowerFxEngine>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockUserManager = new Mock<IUserManager>(MockBehavior.Strict);
            MockLoggerProvider = new Mock<ILoggerProvider>(MockBehavior.Strict);
            MockTestState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockUrlMapper = new Mock<IUrlMapper>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Loose);
            MockTestLogger = new Mock<ITestLogger>(MockBehavior.Strict);
        }

        private void SetupMocks(string testRunId, string testId, string appUrl, TestSuiteDefinition testSuiteDefinition, bool powerFxTestSuccess, string[]? additionalFiles)
        {
            LoggingTestHelper.SetupMock(MockLogger);

            MockTestReporter.Setup(x => x.CreateTest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(testId);
            MockTestReporter.Setup(x => x.StartTest(It.IsAny<string>(), It.IsAny<string>()));
            MockTestReporter.Setup(x => x.EndTest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string?>(), It.IsAny<string?>()));

            MockLoggerProvider.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            MockTestState.Setup(x => x.SetLogger(It.IsAny<ILogger>()));
            MockTestState.Setup(x => x.SetTestSuiteDefinition(It.IsAny<TestSuiteDefinition>()));
            MockTestState.Setup(x => x.SetTestRunId(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetTestId(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetTestResultsDirectory(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetBrowserConfig(It.IsAny<BrowserConfiguration>()));
            MockTestState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);

            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            MockFileSystem.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(additionalFiles);

            MockPowerFxEngine.Setup(x => x.Setup());
            MockPowerFxEngine.Setup(x => x.UpdatePowerFxModelAsync()).Returns(Task.CompletedTask);
            if (powerFxTestSuccess)
            {
                MockPowerFxEngine.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            }
            else
            {
                MockPowerFxEngine.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<string>())).Throws(new Exception("something bad happened"));
            }

            MockTestInfraFunctions.Setup(x => x.SetupAsync(It.IsAny<ILogger>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.SetupNetworkRequestMockAsync(It.IsAny<ILogger>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>(), It.IsAny<ILogger>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.EndTestRunAsync()).Returns(Task.CompletedTask);

            MockUserManager.Setup(x => x.LoginAsUserAsync(It.IsAny<ILogger>())).Returns(Task.CompletedTask);

            MockUrlMapper.Setup(x => x.GenerateTestUrl(It.IsAny<ILogger>())).Returns(appUrl);

            MockTestLogger.Setup(x => x.WriteToLogsFile(It.IsAny<string>()));
            TestLoggerProvider.TestLoggers.Add(testRunId, MockTestLogger.Object);
            TestLoggerProvider.TestLoggers.Add(testId, MockTestLogger.Object);
        }

        private void VerifyTestStateSetup(string testRunId, TestSuiteDefinition testSuiteDefinition, string testResultDirectory, BrowserConfiguration browserConfig)
        {
            MockLoggerProvider.Verify(x => x.CreateLogger(testRunId), Times.Once());
            MockTestState.Verify(x => x.SetTestSuiteDefinition(testSuiteDefinition), Times.Once());
            MockTestState.Verify(x => x.SetTestRunId(testRunId), Times.Once());
            MockTestState.Verify(x => x.SetBrowserConfig(browserConfig));
            MockTestState.Verify(x => x.SetTestResultsDirectory(testResultDirectory), Times.Once());
            MockFileSystem.Verify(x => x.CreateDirectory(testResultDirectory), Times.Once());
        }

        private void VerifySuccessfulTestExecution(string testResultDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig,
            string testRunId, string testId, bool testSuccess, string[]? additionalFiles, string? errorMessage, string? stackTrace, string appUrl)
        {
            MockPowerFxEngine.Verify(x => x.Setup(), Times.Once());
            MockPowerFxEngine.Verify(x => x.UpdatePowerFxModelAsync(), Times.Once());
            MockTestInfraFunctions.Verify(x => x.SetupAsync(It.IsAny<ILogger>()), Times.Once());
            MockUserManager.Verify(x => x.LoginAsUserAsync(It.IsAny<ILogger>()), Times.Once());
            MockTestInfraFunctions.Verify(x => x.SetupNetworkRequestMockAsync(It.IsAny<ILogger>()), Times.Once());
            MockUrlMapper.Verify(x => x.GenerateTestUrl(It.IsAny<ILogger>()), Times.Once());
            MockTestInfraFunctions.Verify(x => x.GoToUrlAsync(appUrl, It.IsAny<ILogger>()), Times.Once());
            MockTestState.Verify(x => x.GetTestSuiteDefinition(), Times.Once());
            MockTestReporter.Verify(x => x.CreateTest(testRunId, testSuiteDefinition.TestCases[0].TestCaseName, "TODO"), Times.Once());
            MockTestReporter.Verify(x => x.StartTest(testRunId, testId), Times.Once());
            MockTestState.Verify(x => x.SetTestId(testId), Times.Once());
            MockLoggerProvider.Verify(x => x.CreateLogger(testId), Times.Once());
            MockTestState.Verify(x => x.SetLogger(It.IsAny<ILogger>()), Times.Exactly(2));
            MockTestState.Verify(x => x.SetTestResultsDirectory(testResultDirectory), Times.Once());
            MockFileSystem.Verify(x => x.CreateDirectory(testResultDirectory), Times.Once());
            MockTestLogger.Verify(x => x.WriteToLogsFile(testResultDirectory), Times.Once());
            MockFileSystem.Verify(x => x.GetFiles(testResultDirectory), Times.Once());
            var additionalFilesList = new List<string>();
            if (additionalFiles != null)
            {
                additionalFilesList = additionalFiles.ToList();
            }
            MockTestReporter.Verify(x => x.EndTest(testRunId, testId, testSuccess, It.Is<string>(x => x.Contains(testSuiteDefinition.TestCases[0].TestCaseName) && x.Contains(browserConfig.Browser)), additionalFilesList, errorMessage, stackTrace), Times.Once());
        }

        private void VerifyFinallyExecution(string testResultDirectory)
        {
            MockTestInfraFunctions.Verify(x => x.EndTestRunAsync(), Times.Once());
            MockTestLogger.Verify(x => x.WriteToLogsFile(testResultDirectory), Times.Once());
        }

        [Theory]
        [InlineData("Null additional files", null)]
        [InlineData("No additional files", new string[] { })]
        [InlineData("Some additional files", new string[] { "/logs.txt", "/screenshot1.png", "/screenshot2.png" })]
        public async Task SingleTestRunnerSuccessTest(string[]? additionalFiles)
        {
            // testName param is present but not used because InlineData requires a primitive parameter as the first one, you can't just use an array
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockLoggerProvider.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object);

            var testData = new TestData();

            SetupMocks(testData.testRunId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, additionalFiles);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig);

            VerifyTestStateSetup(testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig);
            VerifySuccessfulTestExecution(testData.testCaseResultDirectory, testData.testSuiteDefinition, testData.browserConfig, testData.testRunId, testData.testId, true, additionalFiles, null, null, testData.appUrl);
            VerifyFinallyExecution(testData.testResultDirectory);
        }

        [Fact]
        public async Task SingleTestRunnerCanOnlyBeRunOnce()
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockLoggerProvider.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object);

            var testData = new TestData();

            SetupMocks(testData.testRunId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, testData.additionalFiles);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => { await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig); });
        }

        public async Task SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper(Action<Exception> additionalMockSetup)
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                           MockPowerFxEngine.Object,
                                                           MockTestInfraFunctions.Object,
                                                           MockUserManager.Object,
                                                           MockLoggerProvider.Object,
                                                           MockTestState.Object,
                                                           MockUrlMapper.Object,
                                                           MockFileSystem.Object);

            var testData = new TestData();

            SetupMocks(testData.testRunId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, testData.additionalFiles);

            var exceptionToThrow = new InvalidOperationException("Test exception");
            additionalMockSetup(exceptionToThrow);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig);

            VerifyTestStateSetup(testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig);
            LoggingTestHelper.VerifyLogging(MockLogger, exceptionToThrow.ToString(), LogLevel.Error, Times.Once());
            VerifyFinallyExecution(testData.testResultDirectory);
        }

        [Fact]
        public async Task CreateDirectoryThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task PowerFxSetupThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockPowerFxEngine.Setup(x => x.Setup()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task PowerFxUpdatePowerFxModelAsyncThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockPowerFxEngine.Setup(x => x.UpdatePowerFxModelAsync()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task TestInfraSetupThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockTestInfraFunctions.Setup(x => x.SetupAsync(It.IsAny<ILogger>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task LoginAsUserThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockUserManager.Setup(x => x.LoginAsUserAsync(It.IsAny<ILogger>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task SetupNetworkRequestMockThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockTestInfraFunctions.Setup(x => x.SetupNetworkRequestMockAsync(It.IsAny<ILogger>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task GenerateAppUrlThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockUrlMapper.Setup(x => x.GenerateTestUrl(It.IsAny<ILogger>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task GoToUrlThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>(), It.IsAny<ILogger>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task PowerFxExecuteThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockPowerFxEngine.Setup(x => x.Execute(It.IsAny<string>())).Throws(exceptionToThrow);
            });
        }

        class TestData
        {
            public string testRunId;
            public string testRunDirectory;
            public TestSuiteDefinition testSuiteDefinition;
            public BrowserConfiguration browserConfig;

            public string testId;
            public string appUrl;
            public string testResultDirectory;
            public string testCaseResultDirectory;
            public string[] additionalFiles;

            public TestData()
            {
                testRunId = Guid.NewGuid().ToString();
                testRunDirectory = "TestRunDirectory";
                testSuiteDefinition = new TestSuiteDefinition()
                {
                    TestSuiteName = "Test1",
                    TestSuiteDescription = "First test",
                    AppLogicalName = "logicalAppName1",
                    Persona = "User1",
                    OnTestCaseStart = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")",
                    OnTestCaseComplete = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")",
                    OnTestSuiteComplete = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")",
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
                browserConfig = new BrowserConfiguration()
                {
                    Browser = "Chromium"
                };

                testId = Guid.NewGuid().ToString();
                appUrl = "https://fake-app-url.com";
                testResultDirectory = Path.Combine(testRunDirectory, $"{testSuiteDefinition.TestSuiteName}_{browserConfig.Browser}");
                testCaseResultDirectory = Path.Combine(testResultDirectory, $"{testSuiteDefinition.TestCases[0].TestCaseName}_{testId.Substring(0, 6)}");
                additionalFiles = new string[] { };
            }
        }
    }
}
