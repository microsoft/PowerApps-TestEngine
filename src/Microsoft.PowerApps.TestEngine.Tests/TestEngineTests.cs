// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Reporting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        public TestEngineTests()
        {
            MockState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestReporter = new Mock<ITestReporter>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestRunner = new Mock<ISingleTestRunner>(MockBehavior.Strict); 
            ServiceProvider = new ServiceCollection()
                            .AddSingleton(MockSingleTestRunner.Object)
                            .BuildServiceProvider();
        }

        [Fact]
        public async Task TestEngineWithDefaultParamsTest()
        {
            var testSettings = new TestSettings()
            {
                WorkerCount = 2,
                BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    }
                }
            };
            var testDefinitions = new List<TestDefinition>()
            {
                new TestDefinition()
                {
                    Name = "Test1",
                    Description = "First test",
                    AppLogicalName = "logicalAppName1",
                    Persona = "User1",
                    TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                }
            };
            var testConfigFile = "C:\\testPlan.fx.yaml";
            var environmentId = "defaultEnviroment";
            var tenantId = "tenantId";
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = "TestOutput";
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));
            var expectedCloud = "Prod";

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testDefinitions, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object);
            var testReportPath = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId);

            Assert.Equal(expectedTestReportPath, testReportPath);

            Verify(testConfigFile, environmentId, tenantId, expectedCloud, expectedOutputDirectory, testRunId, testRunDirectory, testDefinitions, testSettings);
        }

        [Fact]
        public async Task RunWorkerCountWithDefaultParamsTest()
        {
            var testSettings = new TestSettings()
            {
                WorkerCount = 2,
                BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    }
                }
            };
            var testDefinitions = new List<TestDefinition>()
            {
                new TestDefinition()
                {
                    Name = "Test1",
                    Description = "First test",
                    AppLogicalName = "logicalAppName1",
                    Persona = "User1",
                    TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                }
            };
           
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = "TestOutput";
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testDefinitions, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object);
            await testEngine.RunTestByWorkerCountAsync(testRunId, testRunDirectory);

            foreach (var testDefinition in testDefinitions)
            {
                foreach (var browserConfig in testSettings.BrowserConfigurations)
                {
                    MockSingleTestRunner.Verify(x => x.RunTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig), Times.Once());
                }
            }
        }

        [Theory]
        [ClassData(typeof(TestDataGenerator))]
        public async Task TestEngineTest(string outputDirectory, string cloud, TestSettings testSettings, List<TestDefinition> testDefinitions)
        {
            var testConfigFile = "C:\\testPlan.fx.yaml";
            var environmentId = "defaultEnviroment";
            var tenantId = "tenantId";
            var testRunId = Guid.NewGuid().ToString();
            var expectedOutputDirectory = outputDirectory;
            if (string.IsNullOrEmpty(expectedOutputDirectory))
            {
                expectedOutputDirectory = "TestOutput";
            }
            var testRunDirectory = Path.Combine(expectedOutputDirectory, testRunId.Substring(0, 6));
            var expectedCloud = cloud;
            if (string.IsNullOrEmpty(expectedCloud))
            {
                expectedCloud = "Prod";
            }

            var expectedTestReportPath = "C:\\test.trx";

            SetupMocks(expectedOutputDirectory, testSettings, testDefinitions, testRunId, expectedTestReportPath);

            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object);
            var testReportPath = await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId, outputDirectory, cloud);

            Assert.Equal(expectedTestReportPath, testReportPath);

            Verify(testConfigFile, environmentId, tenantId, expectedCloud, expectedOutputDirectory, testRunId, testRunDirectory, testDefinitions, testSettings);
        }

        private void SetupMocks(string outputDirectory, TestSettings testSettings, List<TestDefinition> testDefinitions, string testRunId, string testReportPath)
        {
            MockState.Setup(x => x.ParseAndSetTestState(It.IsAny<string>()));
            MockState.Setup(x => x.SetEnvironment(It.IsAny<string>()));
            MockState.Setup(x => x.SetTenant(It.IsAny<string>()));
            MockState.Setup(x => x.SetCloud(It.IsAny<string>()));
            MockState.Setup(x => x.SetOutputDirectory(It.IsAny<string>()));
            MockState.Setup(x => x.GetOutputDirectory()).Returns(outputDirectory);
            MockState.Setup(x => x.GetTestSettings()).Returns(testSettings);
            MockState.Setup(x => x.GetTestDefinitions()).Returns(testDefinitions);
            MockState.Setup(x => x.GetWorkerCount()).Returns(testSettings.WorkerCount);

            MockTestReporter.Setup(x => x.CreateTestRun(It.IsAny<string>(), It.IsAny<string>())).Returns(testRunId);
            MockTestReporter.Setup(x => x.StartTestRun(It.IsAny<string>()));
            MockTestReporter.Setup(x => x.EndTestRun(It.IsAny<string>()));
            MockTestReporter.Setup(x => x.GenerateTestReport(It.IsAny<string>(), It.IsAny<string>())).Returns(testReportPath);

            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));

            MockSingleTestRunner.Setup(x => x.RunTestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TestDefinition>(), It.IsAny<BrowserConfiguration>())).Returns(Task.CompletedTask);
        }


        private void Verify(string testConfigFile, string environmentId, string tenantId, string cloud, 
            string outputDirectory, string testRunId, string testRunDirectory, List<TestDefinition> testDefinitions, TestSettings testSettings)
        {
            MockState.Verify(x => x.ParseAndSetTestState(testConfigFile), Times.Once());
            MockState.Verify(x => x.SetEnvironment(environmentId), Times.Once());
            MockState.Verify(x => x.SetTenant(tenantId), Times.Once());
            MockState.Verify(x => x.SetCloud(cloud), Times.Once());
            MockState.Verify(x => x.SetOutputDirectory(outputDirectory), Times.Once());

            MockTestReporter.Verify(x => x.CreateTestRun("Power Fx Test Runner", "User"), Times.Once());
            MockTestReporter.Verify(x => x.StartTestRun(testRunId), Times.Once());

            MockFileSystem.Verify(x => x.CreateDirectory(testRunDirectory), Times.Once());

            foreach (var testDefinition in testDefinitions)
            {
                foreach (var browserConfig in testSettings.BrowserConfigurations)
                {
                    MockSingleTestRunner.Verify(x => x.RunTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig), Times.Once());
                }
            }

            MockTestReporter.Verify(x => x.EndTestRun(testRunId), Times.Once());
            MockTestReporter.Verify(x => x.GenerateTestReport(testRunId, testRunDirectory), Times.Once());
        }

        [Theory]
        [InlineData("", "defaultEnvironment", "tenantId")]
        [InlineData("C:\\testPlan.fx.yaml", "", "tenantId")]
        [InlineData("C:\\testPlan.fx.yaml", "defaultEnvironment", "")]
        public async Task TestEngineThrowsOnNullArguments(string testConfigFile, string environmentId, string tenantId)
        {
            var testEngine = new TestEngine(MockState.Object, ServiceProvider, MockTestReporter.Object, MockFileSystem.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await testEngine.RunTestAsync(testConfigFile, environmentId, tenantId));
        }

        class TestDataGenerator : TheoryData<string, string, TestSettings, List<TestDefinition>>
        {
            public TestDataGenerator()
            {
                // Simple test
                Add("C:\\testResults",
                    "GCC",
                    new TestSettings()
                    {
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new List<TestDefinition>()
                    {
                        new TestDefinition()
                        {
                            Name = "Test1",
                            Description = "First test",
                            AppLogicalName = "logicalAppName1",
                            Persona = "User1",
                            TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                        }
                    });

                // Simple test with null params
                Add(null,
                    null,
                    new TestSettings()
                    {
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new List<TestDefinition>()
                    {
                        new TestDefinition()
                        {
                            Name = "Test1",
                            Description = "First test",
                            AppLogicalName = "logicalAppName1",
                            Persona = "User1",
                            TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                        }
                    });

                // Simple test with empty string params
                Add("",
                    "",
                    new TestSettings()
                    {
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new List<TestDefinition>()
                    {
                        new TestDefinition()
                        {
                            Name = "Test1",
                            Description = "First test",
                            AppLogicalName = "logicalAppName1",
                            Persona = "User1",
                            TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                        }
                    });

                // Multiple browsers
                Add("C:\\testResults",
                    "Prod",
                    new TestSettings()
                    {
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
                    new List<TestDefinition>()
                    {
                        new TestDefinition()
                        {
                            Name = "Test1",
                            Description = "First test",
                            AppLogicalName = "logicalAppName1",
                            Persona = "User1",
                            TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                        }
                    });

                // Multiple tests
                Add("C:\\testResults",
                    "Prod",
                    new TestSettings()
                    {
                        BrowserConfigurations = new List<BrowserConfiguration>()
                        {
                            new BrowserConfiguration()
                            {
                                Browser = "Chromium"
                            }
                        }
                    },
                    new List<TestDefinition>()
                    {
                        new TestDefinition()
                        {
                            Name = "Test1",
                            Description = "First test",
                            AppLogicalName = "logicalAppName1",
                            Persona = "User1",
                            TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                        },
                        new TestDefinition()
                        {
                            Name = "Test2",
                            Description = "Second test",
                            AppLogicalName = "logicalAppName2",
                            Persona = "User2",
                            TestSteps = "Assert(2 + 1 = 3, \"2 + 1 should be 3 \")"
                        }
                    });

                // Multiple tests and browsers
                Add("C:\\testResults",
                    "Prod",
                    new TestSettings()
                    {
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
                    new List<TestDefinition>()
                    {
                        new TestDefinition()
                        {
                            Name = "Test1",
                            Description = "First test",
                            AppLogicalName = "logicalAppName1",
                            Persona = "User1",
                            TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                        },
                        new TestDefinition()
                        {
                            Name = "Test2",
                            Description = "Second test",
                            AppLogicalName = "logicalAppName2",
                            Persona = "User2",
                            TestSteps = "Assert(2 + 1 = 3, \"2 + 1 should be 3 \")"
                        }
                    });
            }
        }
    }
}