// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerApps.TestEngine.Tests
{
    public class SingleTestRunnerTests
    {

        private Mock<ITestReporter> MockTestReporter;
        private Mock<IPowerFxEngine> MockPowerFxEngine;
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<IUserManager> MockUserManager;
        private Mock<IPowerAppFunctions> MockPowerAppFunctions;
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
            MockPowerAppFunctions = new Mock<IPowerAppFunctions>(MockBehavior.Strict);
            MockLoggerProvider = new Mock<ILoggerProvider>(MockBehavior.Strict);
            MockTestState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockUrlMapper = new Mock<IUrlMapper>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockTestLogger = new Mock<ITestLogger>(MockBehavior.Strict);
        }

        private void SetupMocks(string testId, string appUrl, List<PowerAppControlModel> powerAppObjectModel, 
            TestDefinition testDefinition, bool powerFxTestSuccess, string[]? additionalFiles)
        {
            LoggingTestHelper.SetupMock(MockLogger);

            MockTestReporter.Setup(x => x.CreateTest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(testId);
            MockTestReporter.Setup(x => x.StartTest(It.IsAny<string>(), It.IsAny<string>()));
            MockTestReporter.Setup(x => x.EndTest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string?>(), It.IsAny<string?>()));

            MockLoggerProvider.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            MockTestState.Setup(x => x.SetLogger(It.IsAny<ILogger>()));
            MockTestState.Setup(x => x.SetTestDefinition(It.IsAny<TestDefinition>()));
            MockTestState.Setup(x => x.SetTestRunId(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetTestId(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetTestResultsDirectory(It.IsAny<string>()));
            MockTestState.Setup(x => x.SetBrowserConfig(It.IsAny<BrowserConfiguration>()));
            MockTestState.Setup(x => x.GetTestDefinition()).Returns(testDefinition);

            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            MockFileSystem.Setup(x => x.GetFiles(It.IsAny<string>())).Returns(additionalFiles);

            MockPowerFxEngine.Setup(x => x.Setup());
            MockPowerFxEngine.Setup(x => x.UpdateVariable(It.IsAny<string>(), It.IsAny<IUntypedObject>()));
            if (powerFxTestSuccess)
            {
                MockPowerFxEngine.Setup(x => x.Execute(It.IsAny<string>())).Returns(FormulaValue.NewBlank());
            } 
            else
            {
                MockPowerFxEngine.Setup(x => x.Execute(It.IsAny<string>())).Throws(new Exception("something bad happened"));
            }

            MockTestInfraFunctions.Setup(x => x.SetupAsync()).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockTestInfraFunctions.Setup(x => x.EndTestRunAsync()).Returns(Task.CompletedTask);

            MockUserManager.Setup(x => x.LoginAsUserAsync()).Returns(Task.CompletedTask);

            MockUrlMapper.Setup(x => x.GenerateAppUrl()).Returns(appUrl);

            MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Returns(Task.FromResult(powerAppObjectModel));

            MockTestLogger.Setup(x => x.WriteToLogsFile(It.IsAny<string>()));
            TestLoggerProvider.TestLoggers.Add(testId, MockTestLogger.Object);
        }

        private void VerifyTestStateSetup(string testRunId, TestDefinition testDefinition, string testId, string testResultDirectory, BrowserConfiguration browserConfig)
        {
            MockTestReporter.Verify(x => x.CreateTest(testRunId, testDefinition.Name, "TODO"), Times.Once());
            MockTestReporter.Verify(x => x.StartTest(testRunId, testId), Times.Once());
            MockLoggerProvider.Verify(x => x.CreateLogger(testId), Times.Once());
            MockTestState.Verify(x => x.SetLogger(MockLogger.Object), Times.Once());
            MockTestState.Verify(x => x.SetTestDefinition(testDefinition), Times.Once());
            MockTestState.Verify(x => x.SetTestRunId(testRunId), Times.Once());
            MockTestState.Verify(x => x.SetTestId(testId), Times.Once());
            MockTestState.Verify(x => x.SetTestResultsDirectory(testResultDirectory), Times.Once());
            MockTestState.Verify(x => x.SetBrowserConfig(browserConfig));
        }

        private void VerifySuccessfulTestExecution(string testResultDirectory, string appUrl, List<PowerAppControlModel> powerAppObjectModel)
        {
            MockFileSystem.Verify(x => x.CreateDirectory(testResultDirectory), Times.Once());
            MockPowerFxEngine.Verify(x => x.Setup(), Times.Once());
            MockTestInfraFunctions.Verify(x => x.SetupAsync(), Times.Once());
            MockUserManager.Verify(x => x.LoginAsUserAsync(), Times.Once());
            MockUrlMapper.Verify(x => x.GenerateAppUrl(), Times.Once());
            MockTestInfraFunctions.Verify(x => x.GoToUrlAsync(appUrl), Times.Once());
            MockPowerAppFunctions.Verify(x => x.LoadPowerAppsObjectModelAsync(), Times.Once());
            foreach (var control in powerAppObjectModel)
            {
                MockPowerFxEngine.Verify(x => x.UpdateVariable(control.Name, control), Times.Once());
            }
            MockTestState.Verify(x => x.GetTestDefinition(), Times.Once());
        }

        private void VerifyFinallyExecution(string testResultDirectory, TestDefinition testDefinition, BrowserConfiguration browserConfig,
            string testRunId, string testId, bool testSuccess, string[]? additionalFiles, string? errorMessage, string? stackTrace)
        {
            MockTestInfraFunctions.Verify(x => x.EndTestRunAsync(), Times.Once());
            MockTestLogger.Verify(x => x.WriteToLogsFile(testResultDirectory), Times.Once());
            MockFileSystem.Verify(x => x.GetFiles(testResultDirectory), Times.Once());
            var additionalFilesList = new List<string>();
            if (additionalFiles != null)
            {
                additionalFilesList = additionalFiles.ToList();
            }
            MockTestReporter.Verify(x => x.EndTest(testRunId, testId, testSuccess, It.Is<string>(x => x.Contains(testDefinition.Name) && x.Contains(browserConfig.Browser)), additionalFilesList, errorMessage, stackTrace), Times.Once());
        }

        [Theory]
        [InlineData("Null additional files", null)]
        [InlineData("No additional files", new string[] { })]
        [InlineData("Some additional files", new string[] { "/logs.txt", "/screenshot1.png", "/screenshot2.png" })]
        public async Task SingleTestRunnerSuccessTest(string testName, string[]? additionalFiles)
        {
            // testName param is present but not used because InlineData requires a primitive parameter as the first one, you can't just use an array
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockPowerAppFunctions.Object,
                                                        MockLoggerProvider.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object);

            var testData = new TestData(MockPowerAppFunctions.Object);

            SetupMocks(testData.testId, testData.appUrl, testData.powerAppObjectModel, testData.testDefinition, true, additionalFiles);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testDefinition, testData.browserConfig);

            VerifyTestStateSetup(testData.testRunId, testData.testDefinition, testData.testId, testData.testResultDirectory, testData.browserConfig);
            VerifySuccessfulTestExecution(testData.testResultDirectory, testData.appUrl, testData.powerAppObjectModel);
            VerifyFinallyExecution(testData.testResultDirectory, testData.testDefinition, testData.browserConfig, testData.testRunId, testData.testId, true, additionalFiles, null, null);
        }

        [Fact]
        public async Task SingleTestRunnerCanOnlyBeRunOnce()
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                        MockPowerFxEngine.Object,
                                                        MockTestInfraFunctions.Object,
                                                        MockUserManager.Object,
                                                        MockPowerAppFunctions.Object,
                                                        MockLoggerProvider.Object,
                                                        MockTestState.Object,
                                                        MockUrlMapper.Object,
                                                        MockFileSystem.Object);

            var testData = new TestData(MockPowerAppFunctions.Object);

            SetupMocks(testData.testId, testData.appUrl, testData.powerAppObjectModel, testData.testDefinition, true, testData.additionalFiles);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testDefinition, testData.browserConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => { await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testDefinition, testData.browserConfig); });
        }

        public async Task SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper(Action<Exception> additionalMockSetup)
        {
            var singleTestRunner = new SingleTestRunner(MockTestReporter.Object,
                                                           MockPowerFxEngine.Object,
                                                           MockTestInfraFunctions.Object,
                                                           MockUserManager.Object,
                                                           MockPowerAppFunctions.Object,
                                                           MockLoggerProvider.Object,
                                                           MockTestState.Object,
                                                           MockUrlMapper.Object,
                                                           MockFileSystem.Object);

            var testData = new TestData(MockPowerAppFunctions.Object);

            SetupMocks(testData.testId, testData.appUrl, testData.powerAppObjectModel, testData.testDefinition, true, testData.additionalFiles);

            var exceptionToThrow = new InvalidOperationException("Test exception");
            additionalMockSetup(exceptionToThrow);

            await singleTestRunner.RunTestAsync(testData.testRunId, testData.testRunDirectory, testData.testDefinition, testData.browserConfig);

            VerifyTestStateSetup(testData.testRunId, testData.testDefinition, testData.testId, testData.testResultDirectory, testData.browserConfig);
            LoggingTestHelper.VerifyLogging(MockLogger, exceptionToThrow.ToString(), LogLevel.Error, Times.Once());
            VerifyFinallyExecution(testData.testResultDirectory, testData.testDefinition, testData.browserConfig, testData.testRunId, testData.testId, false, testData.additionalFiles, exceptionToThrow.Message, exceptionToThrow.StackTrace);
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
                MockUserManager.Setup(x => x.LoginAsUserAsync()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task GenerateAppUrlThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockUrlMapper.Setup(x => x.GenerateAppUrl()).Throws(exceptionToThrow);
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
        public async Task LoadPowerAppObjectModelThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockPowerAppFunctions.Setup(x => x.LoadPowerAppsObjectModelAsync()).Throws(exceptionToThrow);
            });
        }

        [Fact]
        public async Task PowerFxUpdateVariableThrowsTest()
        {
            await SingleTestRunnerHandlesExceptionsThrownCorrectlyHelper((Exception exceptionToThrow) =>
            {
                MockPowerFxEngine.Setup(x => x.UpdateVariable(It.IsAny<string>(), It.IsAny<PowerAppControlModel>())).Throws(exceptionToThrow);
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
            public TestDefinition testDefinition;
            public BrowserConfiguration browserConfig;

            public string testId;
            public string appUrl;
            public string testResultDirectory;

            public List<PowerAppControlModel> powerAppObjectModel;
            public string[] additionalFiles;

            public Dictionary<string, FormulaType> GenerateProperties()
            {
                return new Dictionary<string, FormulaType>()
                { 
                    { "Text", FormulaType.String },
                    { "Color", FormulaType.Color },
                    { "X", FormulaType.Number },
                    { "Y", FormulaType.Number }
                };

            }

            public TestData(IPowerAppFunctions powerAppFunctions)
            {
                testRunId = Guid.NewGuid().ToString();
                testRunDirectory = "TestRunDirectory";
                testDefinition = new TestDefinition()
                {
                    Name = "Test1",
                    Description = "First test",
                    AppLogicalName = "logicalAppName1",
                    Persona = "User1",
                    TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                };
                browserConfig = new BrowserConfiguration()
                {
                    Browser = "Chromium"
                };

                testId = Guid.NewGuid().ToString();
                appUrl = "https://fake-app-url.com";
                testResultDirectory = Path.Combine(testRunDirectory, $"{testDefinition.Name}_{testId.Substring(0, 6)}");

                powerAppObjectModel = new List<PowerAppControlModel>()
                {
                    new PowerAppControlModel("Label1", GenerateProperties(), powerAppFunctions),
                    new PowerAppControlModel("Label2", GenerateProperties(), powerAppFunctions),
                    new PowerAppControlModel("Button1", GenerateProperties(), powerAppFunctions),
                };
                additionalFiles = new string[] { };
            }
        }
    }
}
