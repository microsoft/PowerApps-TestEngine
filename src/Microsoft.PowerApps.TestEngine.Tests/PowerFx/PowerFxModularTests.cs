// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class PowerFxModularTests
    {
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ILogger> MockLogger;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private string TestFilePath = Path.Combine("C:", "TestPath", "main.yaml");
        private string TestDirectory = Path.Combine("C:", "TestPath");

        public PowerFxModularTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);

            LoggingTestHelper.SetupMock(MockLogger);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
        }

        [Fact]
        public void TestState_ProcessesPowerFxDefinitions()
        {
            // Arrange
            var mainYamlContent = @"
powerFxTestTypes:
  - name: MainType
    value: '{Name: Text}'
powerFxDefinitions:
  - location: 'nested.yaml'
";
            var nestedYamlContent = @"
powerFxTestTypes:
  - name: NestedType
    value: '{Id: Number}'
testFunctions:
  - code: 'NestedFunction(): Text = ""From nested file"";'
";
            string nestedFilePath = Path.Combine(TestDirectory, "nested.yaml");


            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(TestFilePath)).Returns(mainYamlContent);

            MockFileSystem.Setup(fs => fs.FileExists(nestedFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(nestedFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(nestedFilePath)).Returns(nestedYamlContent);

            var testConfigParser = new Mock<ITestConfigParser>(MockBehavior.Strict);
            testConfigParser.Setup(x => x.ParseTestConfig<TestSettings>(TestFilePath, MockLogger.Object))
                .Returns(new TestSettings
                {
                    PowerFxTestTypes = new List<PowerFxTestType> {
                        new PowerFxTestType { Name = "MainType", Value = "{Name: Text}" }
                    },
                    PowerFxDefinitions = new List<PowerFxDefinition> {
                        new PowerFxDefinition { Location = "nested.yaml" }
                    }
                });

            var testState = new TestState(testConfigParser.Object, MockFileSystem.Object);

            // Act
            testState.SetTestConfigFile(new FileInfo(TestFilePath));
            var settings = testState.GetTestSettingsFromFile(TestFilePath, MockLogger.Object);

            // Assert
            Assert.Equal(2, settings.PowerFxTestTypes.Count);
            Assert.Equal("MainType", settings.PowerFxTestTypes[0].Name);
            Assert.Equal("NestedType", settings.PowerFxTestTypes[1].Name);

            Assert.Single(settings.TestFunctions);
            Assert.Equal("NestedFunction(): Text = \"From nested file\";", settings.TestFunctions[0].Code);
        }

        [Fact]
        public void TestState_HandlesCircularReferences()
        {
            // Arrange
            var mainYamlContent = @"
powerFxTestTypes:
  - name: MainType
    value: '{Name: Text}'
powerFxDefinitions:
  - location: 'circular1.yaml'
";
            var circular1YamlContent = @"
powerFxTestTypes:
  - name: Circular1Type
    value: '{Id: Number}'
powerFxDefinitions:
  - location: 'circular2.yaml'
";
            var circular2YamlContent = @"
powerFxTestTypes:
  - name: Circular2Type
    value: '{Value: Boolean}'
powerFxDefinitions:
  - location: 'circular1.yaml'
";
            string circular1FilePath = Path.Combine(TestDirectory, "circular1.yaml");
            string circular2FilePath = Path.Combine(TestDirectory, "circular2.yaml");

            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(TestFilePath)).Returns(mainYamlContent);

            MockFileSystem.Setup(fs => fs.FileExists(circular1FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(circular1FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(circular1FilePath)).Returns(circular1YamlContent);

            MockFileSystem.Setup(fs => fs.FileExists(circular2FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(circular2FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(circular2FilePath)).Returns(circular2YamlContent);

            var testConfigParser = new Mock<ITestConfigParser>(MockBehavior.Strict);
            testConfigParser.Setup(x => x.ParseTestConfig<TestSettings>(TestFilePath, MockLogger.Object))
                .Returns(new TestSettings
                {
                    PowerFxTestTypes = new List<PowerFxTestType> {
                        new PowerFxTestType { Name = "MainType", Value = "{Name: Text}" }
                    },
                    PowerFxDefinitions = new List<PowerFxDefinition> {
                        new PowerFxDefinition { Location = "circular1.yaml" }
                    }
                });

            var testState = new TestState(testConfigParser.Object, MockFileSystem.Object);

            // Act
            testState.SetTestConfigFile(new FileInfo(TestFilePath));
            var settings = testState.GetTestSettingsFromFile(TestFilePath, MockLogger.Object);

            // Assert
            Assert.Equal(3, settings.PowerFxTestTypes.Count);
            Assert.Equal("MainType", settings.PowerFxTestTypes[0].Name);
            Assert.Equal("Circular1Type", settings.PowerFxTestTypes[1].Name);
            Assert.Equal("Circular2Type", settings.PowerFxTestTypes[2].Name);

            // Circular reference prevention is working if we don't have infinite types
        }

        [Fact]
        public void PowerFxEngine_HandlesDuplicateTypes()
        {
            // Arrange
            var settings = new TestSettings
            {
                PowerFxTestTypes = new List<PowerFxTestType>
                {
                    new PowerFxTestType { Name = "DuplicateType", Value = "{FirstValue: Text}" },
                    new PowerFxTestType { Name = "DuplicateType", Value = "{SecondValue: Number}" },
                    new PowerFxTestType { Name = "UniqueType", Value = "{Value: Boolean}" }
                }
            };

            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            MockTestState.Setup(x => x.GetTestSettings()).Returns(settings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");
            MockTestState.Setup(x => x.TestProvider).Returns((ITestWebProvider)null);

            var mockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            var mockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);

            // Act
            var powerFxEngine = new PowerFxEngine(mockTestInfraFunctions.Object, mockTestWebProvider.Object,
                MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, mockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(settings);

            // Assert
            // If no exception is thrown, the test passes - the engine should log warnings but not fail
            LoggingTestHelper.VerifyLogging(MockLogger, "Skipping duplicate type definition: DuplicateType", LogLevel.Warning, Times.Once());
        }

        [Fact]
        public void PowerFxEngine_HandlesDuplicateFunctions()
        {
            // Arrange
            var settings = new TestSettings
            {
                TestFunctions = new List<TestFunction>
                {
                    new TestFunction { Code = "MyDuplicate(x: Text): Text = \"First\" & x;" },
                    new TestFunction { Code = "MyDuplicate(y: Text): Text = \"Second\" & y;" },
                    new TestFunction { Code = "UniqueFunction(): Number = 42;" }
                }
            };

            MockTestState.Setup(x => x.GetTimeout()).Returns(30000);
            MockTestState.Setup(x => x.GetTestSettings()).Returns(settings);
            MockTestState.Setup(x => x.GetTestEngineModules()).Returns(new List<ITestEngineModule>());
            MockTestState.Setup(x => x.GetDomain()).Returns("https://contoso.crm.dynamics.com");
            MockTestState.Setup(x => x.TestProvider).Returns((ITestWebProvider)null);

            var mockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            var mockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            var mockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);

            // Act
            var powerFxEngine = new PowerFxEngine(mockTestInfraFunctions.Object, mockTestWebProvider.Object,
                MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object, mockEnvironmentVariable.Object);

            powerFxEngine.GetAzureCliHelper = () => null;
            powerFxEngine.Setup(settings);

            // Assert
            // If no exception is thrown, the test passes - the engine should log warnings but not fail
            LoggingTestHelper.VerifyLogging(MockLogger, "Skipping duplicate function definition: MyDuplicate", LogLevel.Warning, Times.Once());
        }
    }
}
