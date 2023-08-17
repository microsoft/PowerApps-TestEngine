// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests
{
    public class TestEngineTests
    {
        private Mock<ITestState> MockState;
        private Mock<ITestReporter> MockTestReporter;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ISingleTestRunner> MockSingleTestRunner;
        private IServiceProvider ServiceProvider;
        private Mock<ILoggerFactory> MockLoggerFactory;
        private Mock<ILogger> MockLogger;
        private Mock<ITestEngineEvents> MockTestEngineEventHandler;
        private Mock<ILoggerProvider> MockTestLoggerProvider;

        public TestEngineTests()
        {
            MockState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestReporter = new Mock<ITestReporter>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestRunner = new Mock<ISingleTestRunner>(MockBehavior.Strict);
            ServiceProvider = new ServiceCollection()
                            .AddSingleton(MockSingleTestRunner.Object)
                            .BuildServiceProvider();
            MockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockTestEngineEventHandler = new Mock<ITestEngineEvents>(MockBehavior.Strict);
            MockTestLoggerProvider = new Mock<ILoggerProvider>(MockBehavior.Strict);
        }

        [Fact]
        public async Task TestEngineWithDefaultParamsTest()
        {
            var testSettings = new TestSettings()
            {
                Locale = "en-US",
                BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    }
                }
            };
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
            var testConfigFile = new FileInfo("C:\\testPlan.fx.yaml");
            var environmentId = "defaultEnviroment";
            var tenantId = new Guid("a01af035-a529-4aaf-aded-011ad676f976");
            var outputDirectory = new DirectoryInfo("TestOutput");
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = outputDirectory.FullName;
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));
            var domain = "apps.powerapps.com";

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testSuiteDefinition, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            var testReportPath = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, "");

            Assert.Equal(expectedTestReportPath, testReportPath);

            Verify(testConfigFile.FullName, environmentId, tenantId.ToString(), domain, "", expectedOutputDirectory, testRunId, testRunDirectory, testSuiteDefinition, testSettings);
        }

        [Fact]
        public async Task TestEngineWithInvalidLocaleTest()
        {
            var testSettings = new TestSettings()
            {
                Locale = "de=DEE",     // in case user enters a typo
                BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    }
                }
            };
            var testSuiteDefinition = GetDefaultTestSuiteDefinition();
            var testConfigFile = new FileInfo("C:\\testPlan.fx.yaml");
            var environmentId = "defaultEnviroment";
            var tenantId = new Guid("a01af035-a529-4aaf-aded-011ad676f976");
            var outputDirectory = new DirectoryInfo("TestOutput");
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = outputDirectory.FullName;
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));
            var domain = "apps.powerapps.com";

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testSuiteDefinition, testRunId, expectedTestReportPath);

            var exceptionToThrow = new UserInputException();
            MockState.Setup(x => x.ParseAndSetTestState(testConfigFile.FullName, MockLogger.Object)).Throws(exceptionToThrow);
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);

            var testResultsDirectory = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, "");
            // UserInput Exception is handled within TestEngineEventHandler, and then returns the test results directory path
            MockTestEngineEventHandler.Verify(x => x.EncounteredException(exceptionToThrow), Times.Once());
            Assert.NotNull(testResultsDirectory);
        }

        [Fact]
        public async Task TestEngineWithUnspecifiedLocaleShowsWarning()
        {
            var testSettings = new TestSettings()
            {
                BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    }
                }
            };
            var testSuiteDefinition = GetDefaultTestSuiteDefinition();
            var testConfigFile = new FileInfo("C:\\testPlan.fx.yaml");
            var environmentId = "defaultEnviroment";
            var tenantId = new Guid("a01af035-a529-4aaf-aded-011ad676f976");
            var outputDirectory = new DirectoryInfo("TestOutput");
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = outputDirectory.FullName;
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));
            var domain = "apps.powerapps.com";

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testSuiteDefinition, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            var testReportPath = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, "");

            Assert.Equal(expectedTestReportPath, testReportPath);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Locale property not specified in testSettings. Using current system locale: {CultureInfo.CurrentCulture.Name}", LogLevel.Debug, Times.Once());

            Verify(testConfigFile.FullName, environmentId, tenantId.ToString(), domain, "", expectedOutputDirectory, testRunId, testRunDirectory, testSuiteDefinition, testSettings);
        }

        [Fact]
        public async Task TestEngineWithMultipleBrowserConfigTest()
        {
            var testSettings = new TestSettings()
            {
                Locale = "en-US",
                BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    },
                    new BrowserConfiguration()
                    {
                        Browser = "Firefox"
                    }
                }
            };
            var testSuiteDefinition = GetDefaultTestSuiteDefinition();
            var testConfigFile = new FileInfo("C:\\testPlan.fx.yaml");
            var environmentId = "defaultEnviroment";
            var tenantId = new Guid("a01af035-a529-4aaf-aded-011ad676f976");
            var outputDirectory = new DirectoryInfo("TestOutput");
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = outputDirectory.FullName;
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));
            var domain = "apps.powerapps.com";

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testSuiteDefinition, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            var testReportPath = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, "");

            Assert.Equal(expectedTestReportPath, testReportPath);

            Verify(testConfigFile.FullName, environmentId, tenantId.ToString(), domain, "", expectedOutputDirectory, testRunId, testRunDirectory, testSuiteDefinition, testSettings);
        }

        private TestSuiteDefinition GetDefaultTestSuiteDefinition()
        {
            return new TestSuiteDefinition()
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
        }

        [Theory]
        [ClassData(typeof(TestDataGenerator))]
        public async Task TestEngineTest(DirectoryInfo outputDirectory, string domain, TestSettings testSettings, TestSuiteDefinition testSuiteDefinition)
        {
            var testConfigFile = new FileInfo("C:\\testPlan.fx.yaml");
            var environmentId = "defaultEnviroment";
            var tenantId = new Guid("a01af035-a529-4aaf-aded-011ad676f976");
            var testRunId = Guid.NewGuid().ToString();

            var expectedOutputDirectory = outputDirectory;
            if (expectedOutputDirectory == null)
            {
                expectedOutputDirectory = new DirectoryInfo("TestOutput");
            }
            var testRunDirectory = Path.Combine(expectedOutputDirectory.FullName, testRunId.Substring(0, 6));

            if (string.IsNullOrEmpty(domain))
            {
                domain = "apps.powerapps.com";
            }

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory.FullName, testSettings, testSuiteDefinition, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            var testReportPath = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, "");

            Assert.Equal(expectedTestReportPath, testReportPath);

            Verify(testConfigFile.FullName, environmentId, tenantId.ToString(), domain, "", expectedOutputDirectory.FullName, testRunId, testRunDirectory, testSuiteDefinition, testSettings);
        }

        private void SetupMocks(string outputDirectory, TestSettings testSettings, TestSuiteDefinition testSuiteDefinition, string testRunId, string testReportPath)
        {
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            MockState.Setup(x => x.ParseAndSetTestState(It.IsAny<string>(), MockLogger.Object));
            MockState.Setup(x => x.SetEnvironment(It.IsAny<string>()));
            MockState.Setup(x => x.SetTenant(It.IsAny<string>()));
            MockState.Setup(x => x.SetDomain(It.IsAny<string>()));
            MockState.Setup(x => x.SetOutputDirectory(It.IsAny<string>()));
            MockState.Setup(x => x.GetOutputDirectory()).Returns(outputDirectory);
            MockState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);

            MockTestReporter.Setup(x => x.CreateTestRun(It.IsAny<string>(), It.IsAny<string>())).Returns(testRunId);
            MockTestReporter.Setup(x => x.StartTestRun(It.IsAny<string>()));
            MockTestReporter.Setup(x => x.EndTestRun(It.IsAny<string>()));
            MockTestReporter.Setup(x => x.GenerateTestReport(It.IsAny<string>(), It.IsAny<string>())).Returns(testReportPath);

            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));

            MockSingleTestRunner.Setup(x => x.RunTestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TestSuiteDefinition>(), It.IsAny<BrowserConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CultureInfo>())).Returns(Task.CompletedTask);          
        }


        private void Verify(string testConfigFile, string environmentId, string tenantId, string domain, string queryParams,
            string outputDirectory, string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, TestSettings testSettings)
        {
            MockState.Verify(x => x.ParseAndSetTestState(testConfigFile, MockLogger.Object), Times.Once());
            MockState.Verify(x => x.SetEnvironment(environmentId), Times.Once());
            MockState.Verify(x => x.SetTenant(tenantId), Times.Once());
            MockState.Verify(x => x.SetDomain(domain), Times.Once());
            MockState.Verify(x => x.SetOutputDirectory(outputDirectory), Times.Once());

            MockTestReporter.Verify(x => x.CreateTestRun("Power Fx Test Runner", "User"), Times.Once());
            MockTestReporter.Verify(x => x.StartTestRun(testRunId), Times.Once());

            MockFileSystem.Verify(x => x.CreateDirectory(testRunDirectory), Times.Once());

            var locale = string.IsNullOrEmpty(testSettings.Locale) ? CultureInfo.CurrentCulture : new CultureInfo(testSettings.Locale);

            foreach (var browserConfig in testSettings.BrowserConfigurations)
            {
                MockSingleTestRunner.Verify(x => x.RunTestAsync(testRunId, testRunDirectory, testSuiteDefinition, browserConfig, domain, queryParams, locale), Times.Once());
            }

            MockTestReporter.Verify(x => x.EndTestRun(testRunId), Times.Once());
            MockTestReporter.Verify(x => x.GenerateTestReport(testRunId, testRunDirectory), Times.Once());
        }

        [Theory]
        [InlineData(null, "Default-EnvironmentId", "a01af035-a529-4aaf-aded-011ad676f976", "apps.powerapps.com")]
        [InlineData("C:\\testPlan.fx.yaml", "", "a01af035-a529-4aaf-aded-011ad676f976", "apps.powerapps.com")]
        [InlineData("C:\\testPlan.fx.yaml", "Default-EnvironmentId", "a01af035-a529-4aaf-aded-011ad676f976", "")]
        public async Task TestEngineThrowsOnNullArguments(string testConfigFilePath, string environmentId, Guid tenantId, string domain)
        {
            MockTestReporter.Setup(x => x.CreateTestRun(It.IsAny<string>(), It.IsAny<string>())).Returns(Guid.NewGuid().ToString());
            MockTestReporter.Setup(x => x.StartTestRun(It.IsAny<string>()));
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            MockState.Setup(x => x.SetOutputDirectory(It.IsAny<string>()));
            MockState.Setup(x => x.GetOutputDirectory()).Returns("MockOutputDirectory");
            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);

            FileInfo testConfigFile;
            if (string.IsNullOrEmpty(testConfigFilePath))
            {
                //specifically for the test case where the caller might
                //inadvertently pass a null testConfigFile object to RunTestAsync
                testConfigFile = null;
            }
            else
            {
                testConfigFile = new FileInfo(testConfigFilePath);
            }
            var outputDirectory = new DirectoryInfo("TestOutput");
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, ""));
        }

        [Fact]
        public async Task TestEngineReturnsPathOnUserInputErrors()
        {
            FileInfo testConfigFile = new FileInfo("C:\\testPlan.fx.yaml");
            string environmentId = "defaultEnviroment";
            Guid tenantId = new Guid("a01af035-a529-4aaf-aded-011ad676f976");
            string domain = "apps.powerapps.com";

            MockTestReporter.Setup(x => x.CreateTestRun(It.IsAny<string>(), It.IsAny<string>())).Returns("abcdef");
            MockTestReporter.Setup(x => x.StartTestRun(It.IsAny<string>()));
            MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            MockState.Setup(x => x.SetOutputDirectory(It.IsAny<string>()));
            MockState.Setup(x => x.GetOutputDirectory()).Returns("MockOutputDirectory");            
            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            MockTestLoggerProvider.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(MockLogger.Object);

            var exceptionToThrow = new UserInputException();
            MockState.Setup(x => x.ParseAndSetTestState(testConfigFile.FullName, MockLogger.Object)).Throws(exceptionToThrow);
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            var outputDirectory = new DirectoryInfo("TestOutput");

            var testResultsDirectory = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, domain, "");
            // UserInput Exception is handled within TestEngineEventHandler, and then returns the test results directory path
            MockTestEngineEventHandler.Verify(x => x.EncounteredException(exceptionToThrow), Times.Once());
            Assert.NotNull(testResultsDirectory);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetLocaleFromTestSettingsUseSystemLocaleIfNull(string localeInput)
        {
            // Arrange
            LoggingTestHelper.SetupMock(MockLogger);

            // Act
            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            testEngine.Logger = MockLogger.Object;

            // Assert
            var locale = testEngine.GetLocaleFromTestSettings(localeInput);
            Assert.Equal(CultureInfo.CurrentCulture, locale);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Locale property not specified in testSettings. Using current system locale: {locale.Name}", LogLevel.Debug, Times.Once());
        }

        [Fact]
        public async Task GetLocaleFromTestSettingsThrowsUserInputExceptionOnInvalidLocale()
        {
            // Arrange
            LoggingTestHelper.SetupMock(MockLogger);
            var localeInput = "invalidLocaleName";

            // Act
            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object, MockLoggerFactory.Object, MockTestEngineEventHandler.Object);
            testEngine.Logger = MockLogger.Object;

            // Assert
            var ex = Assert.Throws<UserInputException>(() => testEngine.GetLocaleFromTestSettings(localeInput));
            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings.ToString(), ex.Message);
            LoggingTestHelper.VerifyLogging(MockLogger, $"Locale from test suite definition {localeInput} unrecognized.", LogLevel.Error, Times.Once());
        }

        class TestDataGenerator : TheoryData<DirectoryInfo, string, TestSettings, TestSuiteDefinition>
        {
            public TestDataGenerator()
            {
                // Simple test
                Add(new DirectoryInfo("C:\\testResults"),
                    "GCC",
                    new TestSettings()
                    {
                        Locale = string.Empty,
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new TestSuiteDefinition()
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
                    });

                // Simple test with null params
                Add(new DirectoryInfo("TestOutput"),
                    null,
                    new TestSettings()
                    {
                        Locale = string.Empty,
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new TestSuiteDefinition()
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
                    });

                // Simple test with empty string params
                Add(new DirectoryInfo("TestOutput"),
                    "",
                    new TestSettings()
                    {
                        Locale = string.Empty,
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new TestSuiteDefinition()
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
                    });

                // Simple test in en-US locale (this should be like every other test)
                // For the rest of the tests where Locale = string.Empty, CurrentCulture should be used
                // and the test should pass
                Add(new DirectoryInfo("C:\\testResults"),
                    "GCC",
                    new TestSettings()
                    {
                        Locale = "en-US",
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new TestSuiteDefinition()
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
                    });

                // Simple test in a different locale
                Add(new DirectoryInfo("C:\\testResults"),
                    "GCC",
                    new TestSettings()
                    {
                        Locale = "de-DE",
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new TestSuiteDefinition()
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
                                TestSteps = "Assert(1 + 1 = 2; \"1 + 1 should be 2 \")"
                            }
                        }
                    });

                // Multiple browsers
                Add(new DirectoryInfo("C:\\testResults"),
                    "Prod",
                    new TestSettings()
                    {
                        Locale = string.Empty,
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            },
                            new BrowserConfiguration()
                            {
                                Browser = "Firefox"
                            },
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium",
                                Device = "Pixel 2"
                            }
                        }
                    },
                    new TestSuiteDefinition()
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
                    });

                // Multiple tests
                Add(new DirectoryInfo("C:\\testResults"),
                    "Prod",
                    new TestSettings()
                    {
                        Locale = string.Empty,
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new TestSuiteDefinition()
                    {
                        TestSuiteName = "Test1",
                        TestSuiteDescription = "First test",
                        AppLogicalName = "logicalAppName1",
                        Persona = "User1",
                        TestCases = new List<TestCase>()
                        {
                            new TestCase
                            {
                                TestCaseName = "Test1",
                                TestCaseDescription = "First test",
                                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                            },
                            new TestCase
                            {
                                TestCaseName = "Test2",
                                TestCaseDescription = "Second test",
                                TestSteps = "Assert(2 + 1 = 3, \"2 + 1 should be 3 \")"
                            }
                        }
                    });

                // Multiple tests and browsers
                Add(new DirectoryInfo("C:\\testResults"),
                    "Prod",
                    new TestSettings()
                    {
                        Locale = string.Empty,
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            },
                            new BrowserConfiguration()
                            {
                                Browser = "Firefox"
                            },
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium",
                                Device = "Pixel 2"
                            }
                        }
                    },
                    new TestSuiteDefinition()
                    {
                        TestSuiteName = "Test1",
                        TestSuiteDescription = "First test",
                        AppLogicalName = "logicalAppName1",
                        Persona = "User1",
                        TestCases = new List<TestCase>()
                        {
                            new TestCase
                            {
                                TestCaseName = "Test1",
                                TestCaseDescription = "First test",
                                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                            },
                            new TestCase
                            {
                                TestCaseName = "Test2",
                                TestCaseDescription = "Second test",
                                TestSteps = "Assert(2 + 1 = 3, \"2 + 1 should be 3 \")"
                            }
                        }
                    });
            }
        }
    }
}
