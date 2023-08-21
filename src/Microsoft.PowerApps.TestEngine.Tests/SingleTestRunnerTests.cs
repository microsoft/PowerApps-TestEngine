// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
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
        private Mock<ILoggerFactory> MockLoggerFactory;
        private Mock<ISingleTestInstanceState> MockTestState;
        private Mock<IUrlMapper> MockUrlMapper;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ILogger> MockLogger;
        private Mock<ITestLogger> MockTestLogger;
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
        private Mock<ITestEngineEvents> MockTestEngineEventHandler;

        public SingleTestRunnerTests()
        {
            MockTestReporter = new Mock<ITestReporter>(MockBehavior.Strict);
            MockPowerFxEngine = new Mock<IPowerFxEngine>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockUserManager = new Mock<IUserManager>(MockBehavior.Strict);
            MockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            MockTestState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockUrlMapper = new Mock<IUrlMapper>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockTestLogger = new Mock<ITestLogger>(MockBehavior.Strict);
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockTestEngineEventHandler = new Mock<ITestEngineEvents>(MockBehavior.Strict);
        }

        private void SetupMocks(string testRunId, string testSuiteId, string testId, string appUrl, TestSuiteDefinition testSuiteDefinition, bool powerFxTestSuccess, string[]? additionalFiles, string testSuitelocale)
        {
            LoggingTestHelper.SetupMock(MockLogger);
            MockLogger.Setup(x => x.BeginScope(It.IsAny<string>())).Returns(new TestLoggerScope("", () => { }));

            MockTestReporter.Setup(x => x.CreateTest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(testId);
            MockTestReporter.Setup(x => x.CreateTestSuite(It.IsAny<string>(), It.IsAny<string>())).Returns(testSuiteId);
            MockTestReporter.Setup(x => x.StartTest(It.IsAny<string>(), It.IsAny<string>()));
            MockTestReporter.Setup(x => x.EndTest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>()));
            MockTestReporter.Setup(x => x.FailTest(It.IsAny<string>(), It.IsAny<string>()));
            
            MockTestReporter.SetupSet(x => x.TestResultsDirectory = "TestRunDirectory");
            MockTestReporter.SetupGet(x => x.TestResultsDirectory).Returns("TestRunDirectory");
            MockTestReporter.SetupSet(x => x.TestRunAppURL = "https://fake-app-url.com");
            MockTestReporter.SetupGet(x => x.TestRunAppURL).Returns("https://fake-app-url.com");

            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            MockTestState.Setup(x => x.SetLogger(It.IsAny<ILogger>()));
            MockTestState.Setup(x => x.SetTestSuiteDefinition(It.IsAny<TestSuiteDefinition>()));
            MockTestState.Setup(x => x.SetTestRunId(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetTestId(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetTestResultsDirectory(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetBrowserConfig(It.IsAny<BrowserConfiguration>()));
            MockTestState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockTestState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            MockFileSystem.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(additionalFiles);
            MockFileSystem.Setup(x => x.RemoveInvalidFileNameChars(testSuiteDefinition.TestSuiteName)).Returns(testSuiteDefinition.TestSuiteName);


            var locale = string.IsNullOrEmpty(testSuitelocale) ? CultureInfo.CurrentCulture : new CultureInfo(testSuitelocale);
            MockPowerFxEngine.Setup(x => x.Setup());
            MockPowerFxEngine.Setup(x => x.RunRequirementsCheckAsync()).Returns(Task.CompletedTask);
            MockPowerFxEngine.Setup(x => x.UpdatePowerFxModelAsync()).Returns(Task.CompletedTask);
            MockPowerFxEngine.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<CultureInfo>())).Returns(FormulaValue.NewBlank());

            MockTestEngineEventHandler.Setup(x => x.SetAndInitializeCounters(It.IsAny<int>()));
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            MockTestEngineEventHandler.Setup(x => x.SuiteBegin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            MockTestEngineEventHandler.Setup(x => x.SuiteEnd());
            MockTestEngineEventHandler.Setup(x => x.TestCaseBegin(It.IsAny<string>()));
            MockTestEngineEventHandler.Setup(x => x.TestCaseEnd(It.IsAny<bool>()));

            if (powerFxTestSuccess)
            {
                MockPowerFxEngine.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<string>(), It.IsAny<CultureInfo>())).Returns(Task.CompletedTask);
            }
            else
            {
                MockPowerFxEngine.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<string>(), It.IsAny<CultureInfo>())).Throws(new Exception("something bad happened"));
            }
            MockPowerFxEngine.Setup(x => x.GetPowerAppFunctions()).Returns(MockPowerAppFunctions.Object);

            MockTestInfraFunctions.Setup(x => x.SetupAsync()).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.SetupNetworkRequestMockAsync()).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.EndTestRunAsync()).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.DisposeAsync()).Returns(Task.CompletedTask);

            MockUserManager.Setup(x => x.LoginAsUserAsync(appUrl)).Returns(Task.CompletedTask);

            MockUrlMapper.Setup(x => x.GenerateTestUrl("", "")).Returns(appUrl);

            MockTestLogger.Setup(x => x.WriteToLogsFile(It.IsAny<string>(), It.IsAny<string>()));
            MockTestLogger.Setup(x => x.WriteExceptionToDebugLogsFile(It.IsAny<string>(), It.IsAny<string>()));
            TestLoggerProvider.TestLoggers.Add(testSuiteId, MockTestLogger.Object);
        }

        // When OnTestSuiteComplete exists, the test result directory will be set an extra time. 
        private void VerifyTestStateSetup(string testSuiteId, string testRunId, TestSuiteDefinition testSuiteDefinition, string testResultDirectory, BrowserConfiguration browserConfig, int setDirectoryTimes = 1)
        {
            MockLoggerFactory.Verify(x => x.CreateLogger(testSuiteId), Times.Once());
            MockTestState.Verify(x => x.SetTestSuiteDefinition(testSuiteDefinition), Times.Once());
            MockTestState.Verify(x => x.SetTestRunId(testRunId), Times.Once());
            MockTestState.Verify(x => x.SetBrowserConfig(browserConfig));
            MockTestState.Verify(x => x.SetTestResultsDirectory(testResultDirectory), Times.Exactly(setDirectoryTimes));
            MockFileSystem.Verify(x => x.CreateDirectory(testResultDirectory), Times.Once());
        }

        private void VerifySuccessfulTestExecution(string testResultDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig,
            string testSuiteId, string testRunId, string testId, bool testSuccess, string[]? additionalFiles, string? errorMessage, string? stackTrace, string appUrl, CultureInfo locale)
        {
            MockPowerFxEngine.Verify(x => x.Setup(), Times.Once());
            MockPowerFxEngine.Verify(x => x.UpdatePowerFxModelAsync(), Times.Once());
            MockTestInfraFunctions.Verify(x => x.SetupAsync(), Times.Once());
            MockUserManager.Verify(x => x.LoginAsUserAsync(appUrl), Times.Once());
            MockTestInfraFunctions.Verify(x => x.SetupNetworkRequestMockAsync(), Times.Once());
            MockUrlMapper.Verify(x => x.GenerateTestUrl("", ""), Times.Once());
            MockTestInfraFunctions.Verify(x => x.GoToUrlAsync(appUrl), Times.Once());
            MockTestState.Verify(x => x.GetTestSuiteDefinition(), Times.Exactly(2));
            MockTestReporter.Verify(x => x.CreateTest(testRunId, testSuiteId, testSuiteDefinition.TestCases[0].TestCaseName), Times.Once());
            MockTestReporter.Verify(x => x.StartTest(testRunId, testId), Times.Once());
            MockTestState.Verify(x => x.SetTestId(testId), Times.Once());
            MockLoggerFactory.Verify(x => x.CreateLogger(testSuiteId), Times.Once());
            MockTestState.Verify(x => x.SetLogger(It.IsAny<ILogger>()), Times.Exactly(1));
            MockTestState.Verify(x => x.SetTestResultsDirectory(testResultDirectory), Times.Once());
            MockFileSystem.Verify(x => x.CreateDirectory(testResultDirectory), Times.Once());
            MockTestLogger.Verify(x => x.WriteToLogsFile(testResultDirectory, testId), Times.Once());
            MockFileSystem.Verify(x => x.GetFiles(testResultDirectory), Times.Once());
            MockTestInfraFunctions.Verify(x => x.DisposeAsync(), Times.Once());
            var additionalFilesList = new List<string>();
            if (additionalFiles != null)
            {
                additionalFilesList = additionalFiles.ToList();
            }
            MockTestReporter.Verify(x => x.EndTest(testRunId, testId, testSuccess, It.Is<string>(x => x.Contains(browserConfig.Browser)), additionalFilesList, errorMessage), Times.Once());
        }

        private void VerifyFinallyExecution(string testResultDirectory, int total, int pass, int fail)
        {
            string summaryString = $"\nTest suite summary\nTotal cases: {total}" +
                                $"\nCases passed: {pass}" +
                                $"\nCases failed: {fail}";

            MockTestInfraFunctions.Verify(x => x.EndTestRunAsync(), Times.Once());
            MockTestLogger.Verify(x => x.WriteToLogsFile(testResultDirectory, null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, (string)summaryString, LogLevel.Information, Times.Once());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { } })]
        [InlineData(new object[] { new string[] { "/logs.txt", "/screenshot1.png", "/screenshot2.png" } })]
        public async Task SingleTestRunnerSuccessWithTestDataOneTest(string[]? additionalFiles)
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object,
                                                        MockLoggerFactory.Object,
                                                        MockTestEngineEventHandler.Object);

            var testData = new TestDataOne();

            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, additionalFiles, testData.testSuiteLocale);

            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);
            
            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);

            VerifyTestStateSetup(testData.testSuiteId, testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig, 2);
            VerifySuccessfulTestExecution(testData.testCaseResultDirectory, testData.testSuiteDefinition, testData.browserConfig, testData.testSuiteId, testData.testRunId, testData.testId, true, additionalFiles, null, null, testData.appUrl, locale);
            VerifyFinallyExecution(testData.testResultDirectory, 1, 1, 0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new string[] { } })]
        [InlineData(new object[] { new string[] { "/logs.txt", "/screenshot1.png", "/screenshot2.png" } })]
        public async Task SingleTestRunnerSuccessWithTestDataTwoTest(string[]? additionalFiles)
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object,
                                                        MockLoggerFactory.Object,
                                                        MockTestEngineEventHandler.Object);

            var testData = new TestDataTwo();

            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, additionalFiles, testData.testSuiteLocale);

            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);
            
            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);

            VerifyTestStateSetup(testData.testSuiteId, testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig);
            VerifySuccessfulTestExecution(testData.testCaseResultDirectory, testData.testSuiteDefinition, testData.browserConfig, testData.testSuiteId, testData.testRunId, testData.testId, true, additionalFiles, null, null, testData.appUrl, locale);
            VerifyFinallyExecution(testData.testResultDirectory, 1, 1, 0);
        }

        [Fact]
        public async Task SingleTestRunnerCanOnlyBeRunOnce()
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object,
                                                        MockLoggerFactory.Object,
                                                        MockTestEngineEventHandler.Object);

            var testData = new TestDataOne();

            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, testData.additionalFiles, testData.testSuiteLocale);

            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);
            
            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => { await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale); });
        }

        [Fact]
        public async Task SingleTestRunnerPowerFxTestFail()
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object,
                                                        MockLoggerFactory.Object,
                                                        MockTestEngineEventHandler.Object);

            var testData = new TestDataOne();

            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, false, testData.additionalFiles, testData.testSuiteLocale);

            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);
            
            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);

            VerifyTestStateSetup(testData.testSuiteId, testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig, 2);
            VerifyFinallyExecution(testData.testResultDirectory, 1, 0, 1);
        }

        public async Task SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper(Action<Exception> additionalMockSetup)
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                           MockPowerFxEngine.Object,
                                                           MockTestInfraFunctions.Object,
                                                           MockUserManager.Object,
                                                           MockTestState.Object,
                                                           MockUrlMapper.Object,
                                                           MockFileSystem.Object,
                                                           MockLoggerFactory.Object,
                                                           MockTestEngineEventHandler.Object);

            var testData = new TestDataOne();

            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, testData.additionalFiles, testData.testSuiteLocale);

            var debugObj = new ExpandoObject();
            debugObj.TryAdd("appId", "someAppId");
            debugObj.TryAdd("appVersion", "someAppVersionId");
            debugObj.TryAdd("environmentId", "someEnvironmentId");
            debugObj.TryAdd("sessionId", "someSessionId");

            MockPowerAppFunctions.Setup(x => x.GetDebugInfo()).Returns(Task.FromResult((object)debugObj));

            var exceptionToThrow = new InvalidOperationException("Test exception");
            additionalMockSetup(exceptionToThrow);

            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);
            
            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);

            VerifyTestStateSetup(testData.testSuiteId, testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig);
            LoggingTestHelper.VerifyLogging(MockLogger, "Encountered an error. See the debug log for this test suite for more information.", LogLevel.Error, Times.AtLeastOnce());
            VerifyFinallyExecution(testData.testResultDirectory, 1, 0, 1);
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
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) => {
                MockPowerFxEngine.Setup(x => x.UpdatePowerFxModelAsync()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task TestInfraSetupThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockTestInfraFunctions.Setup(x => x.SetupAsync()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task LoginAsUserThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockUserManager.Setup(x => x.LoginAsUserAsync(It.IsAny<string>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task SetupNetworkRequestMockThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockTestInfraFunctions.Setup(x => x.SetupNetworkRequestMockAsync()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task GenerateAppUrlThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockUrlMapper.Setup(x => x.GenerateTestUrl("", "")).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task GoToUrlThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task PowerFxExecuteThrowsTest()
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                           MockPowerFxEngine.Object,
                                                           MockTestInfraFunctions.Object,
                                                           MockUserManager.Object,
                                                           MockTestState.Object,
                                                           MockUrlMapper.Object,
                                                           MockFileSystem.Object,
                                                           MockLoggerFactory.Object,
                                                           MockTestEngineEventHandler.Object);

            var testData = new TestDataOne();

            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, testData.additionalFiles, testData.testSuiteLocale);

            var exceptionToThrow = new InvalidOperationException("Test exception");

            MockPowerFxEngine.Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<CultureInfo>())).Throws(exceptionToThrow);

            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);

            VerifyTestStateSetup(testData.testSuiteId, testData.testRunId, testData.testSuiteDefinition, testData.testResultDirectory, testData.browserConfig, 2);
            LoggingTestHelper.VerifyLogging(MockLogger, "Encountered an error. See the debug log for this test suite for more information.", LogLevel.Error, Times.Once());
            VerifyFinallyExecution(testData.testResultDirectory, 1, 1, 0);
        }

        [Fact]
        public async Task UserInputExceptionHandlingTest()
        {
            // Arrange
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                           MockPowerFxEngine.Object,
                                                           MockTestInfraFunctions.Object,
                                                           MockUserManager.Object,
                                                           MockTestState.Object,
                                                           MockUrlMapper.Object,
                                                           MockFileSystem.Object,
                                                           MockLoggerFactory.Object,
                                                           MockTestEngineEventHandler.Object);

            var testData = new TestDataOne();
            SetupMocks(testData.testRunId, testData.testSuiteId, testData.testId, testData.appUrl, testData.testSuiteDefinition, true, testData.additionalFiles, testData.testSuiteLocale);
            var locale = string.IsNullOrEmpty(testData.testSuiteLocale) ? CultureInfo.CurrentCulture : new CultureInfo(testData.testSuiteLocale);

            // Specific setup for this test
            var exceptionToThrow = new UserInputException(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString());
            MockUserManager.Setup(x => x.LoginAsUserAsync(It.IsAny<string>())).Throws(exceptionToThrow);
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));            

            // Act
            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testSuiteDefinition, testData.browserConfig, "", "", locale);

            // Assert
            MockTestEngineEventHandler.Verify(x => x.EncounteredException(exceptionToThrow), Times.Once());
        } 
        // Sample Test Data for test with OnTestCaseStart, OnTestCaseComplete and OnTestSuiteComplete
        class TestDataOne
        {
            public string testRunId;
            public string testRunDirectory;
            public TestSuiteDefinition testSuiteDefinition;
            public BrowserConfiguration browserConfig;

            public string testId;
            public string appUrl;
            public string testSuiteId;
            public string testResultDirectory;
            public string testCaseResultDirectory;
            public string[] additionalFiles;
            public string testSuiteLocale;

            public TestDataOne()
            {
                testRunId = Guid.NewGuid().ToString();
                testSuiteId = Guid.NewGuid().ToString();
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
                testResultDirectory = Path.Combine(testRunDirectory, $"{testSuiteDefinition.TestSuiteName}_{browserConfig.Browser}_{testSuiteId.Substring(0, 6)}");
                testCaseResultDirectory = Path.Combine(testResultDirectory, $"{testSuiteDefinition.TestCases[0].TestCaseName}_{testId.Substring(0, 6)}");
                additionalFiles = new string[] { };
                testSuiteLocale = "en-US";
            }
        }

        // Sample Test Data for test
        class TestDataTwo
        {
            public string testRunId;
            public string testSuiteId;
            public string testRunDirectory;
            public TestSuiteDefinition testSuiteDefinition;
            public BrowserConfiguration browserConfig;

            public string testId;
            public string appUrl;
            public string testResultDirectory;
            public string testCaseResultDirectory;
            public string[] additionalFiles;
            public string testSuiteLocale;

            public TestDataTwo()
            {
                testRunId = Guid.NewGuid().ToString();
                testSuiteId = Guid.NewGuid().ToString();
                testRunDirectory = "TestRunDirectory";
                testSuiteDefinition = new TestSuiteDefinition()
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
                browserConfig = new BrowserConfiguration()
                {
                    Browser = "Chromium"
                };

                testId = Guid.NewGuid().ToString();
                appUrl = "https://fake-app-url.com";
                testResultDirectory = Path.Combine(testRunDirectory, $"{testSuiteDefinition.TestSuiteName}_{browserConfig.Browser}_{testSuiteId.Substring(0, 6)}");
                testCaseResultDirectory = Path.Combine(testResultDirectory, $"{testSuiteDefinition.TestCases[0].TestCaseName}_{testId.Substring(0, 6)}");
                additionalFiles = new string[] { };
                testSuiteLocale = "en-US";
            }
        }
    }
}
