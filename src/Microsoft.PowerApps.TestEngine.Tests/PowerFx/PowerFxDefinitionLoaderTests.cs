// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx
{
    public class PowerFxDefinitionLoaderTests
    {
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ILogger> MockLogger;
        private string TestFilePath = Path.Combine("C:", "TestPath", "main.yaml");
        private string TestDirectory = Path.Combine("C:", "TestPath");

        public PowerFxDefinitionLoaderTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            LoggingTestHelper.SetupMock(MockLogger);
        }

        [Fact]
        public void LoadPowerFxDefinitionsFromFile_LoadsTypesAndFunctions()
        {
            // Arrange
            var yamlContent = @"
powerFxTestTypes:
  - name: TestType1
    value: '{Name: Text}'
testFunctions:
  - code: 'MyFunction(param: Text): Text = ""Hello "" & param;'
";

            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(TestFilePath)).Returns(yamlContent);

            var loader = new PowerFxDefinitionLoader(MockFileSystem.Object, MockLogger.Object);
            var settings = new TestSettings();

            // Act
            loader.LoadPowerFxDefinitionsFromFile(TestFilePath, settings);

            // Assert
            Assert.Single(settings.PowerFxTestTypes);
            Assert.Equal("TestType1", settings.PowerFxTestTypes[0].Name);
            Assert.Equal("{Name: Text}", settings.PowerFxTestTypes[0].Value);

            Assert.Single(settings.TestFunctions);
            Assert.Equal("MyFunction(param: Text): Text = \"Hello \" & param;", settings.TestFunctions[0].Code);

            LoggingTestHelper.VerifyLogging(MockLogger, $"Successfully loaded PowerFx definitions from {TestFilePath}", LogLevel.Information, Times.Once());
        }

        [Fact]
        public void LoadPowerFxDefinitionsFromFile_HandlesNestedDefinitions()
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

            var loader = new PowerFxDefinitionLoader(MockFileSystem.Object, MockLogger.Object);
            var settings = new TestSettings();

            // Act
            loader.LoadPowerFxDefinitionsFromFile(TestFilePath, settings);

            // Assert
            Assert.Equal(2, settings.PowerFxTestTypes.Count);
            Assert.Equal("MainType", settings.PowerFxTestTypes[0].Name);
            Assert.Equal("NestedType", settings.PowerFxTestTypes[1].Name);

            Assert.Single(settings.TestFunctions);
            Assert.Equal("NestedFunction(): Text = \"From nested file\";", settings.TestFunctions[0].Code);

            LoggingTestHelper.VerifyLogging(MockLogger, $"Successfully loaded PowerFx definitions from {TestFilePath}", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, $"Successfully loaded PowerFx definitions from {nestedFilePath}", LogLevel.Information, Times.Once());
        }

        [Fact]
        public void LoadPowerFxDefinitionsFromFile_HandlesMultipleLevelsOfNesting()
        {
            // Arrange
            var mainYamlContent = @"
powerFxDefinitions:
  - location: 'level1.yaml'
";
            var level1YamlContent = @"
powerFxTestTypes:
  - name: Level1Type
    value: '{Name: Text}'
powerFxDefinitions:
  - location: 'level2.yaml'
";
            var level2YamlContent = @"
powerFxTestTypes:
  - name: Level2Type
    value: '{Id: Number}'
testFunctions:
  - code: 'Level2Function(): Text = ""From level 2"";'
";
            string level1FilePath = Path.Combine(TestDirectory, "level1.yaml");
            string level2FilePath = Path.Combine(TestDirectory, "level2.yaml");

            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(TestFilePath)).Returns(mainYamlContent);

            MockFileSystem.Setup(fs => fs.FileExists(level1FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(level1FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(level1FilePath)).Returns(level1YamlContent);

            MockFileSystem.Setup(fs => fs.FileExists(level2FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(level2FilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(level2FilePath)).Returns(level2YamlContent);

            var loader = new PowerFxDefinitionLoader(MockFileSystem.Object, MockLogger.Object);
            var settings = new TestSettings();

            // Act
            loader.LoadPowerFxDefinitionsFromFile(TestFilePath, settings);

            // Assert
            Assert.Equal(2, settings.PowerFxTestTypes.Count);
            Assert.Equal("Level1Type", settings.PowerFxTestTypes[0].Name);
            Assert.Equal("Level2Type", settings.PowerFxTestTypes[1].Name);

            Assert.Single(settings.TestFunctions);
            Assert.Equal("Level2Function(): Text = \"From level 2\";", settings.TestFunctions[0].Code);
        }

        [Fact]
        public void LoadPowerFxDefinitionsFromFile_HandlesFileNotFound()
        {
            // Arrange
            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(false);

            var loader = new PowerFxDefinitionLoader(MockFileSystem.Object, MockLogger.Object);
            var settings = new TestSettings();

            // Act & Assert
            loader.LoadPowerFxDefinitionsFromFile(TestFilePath, settings);

            LoggingTestHelper.VerifyLogging(MockLogger, $"PowerFx definition file not found: {TestFilePath}", LogLevel.Error, Times.Once());
        }

        [Fact]
        public void LoadPowerFxDefinitionsFromFile_HandlesInvalidYaml()
        {
            // Arrange
            var invalidYaml = @"
powerFxTestTypes:
  - name: InvalidType
  value: This is not valid YAML
";

            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(TestFilePath)).Returns(invalidYaml);

            var loader = new PowerFxDefinitionLoader(MockFileSystem.Object, MockLogger.Object);
            var settings = new TestSettings();

            // Act & Assert
            loader.LoadPowerFxDefinitionsFromFile(TestFilePath, settings);

            LoggingTestHelper.VerifyLogging(MockLogger, "Error loading PowerFx definitions from C:\\TestPath\\main.yaml: While parsing a block collection, did not find expected '-' indicator.", LogLevel.Error, Times.Once());
        }

        [Fact]
        public void LoadPowerFxDefinitionsFromFile_HandlesRelativePaths()
        {
            // Arrange
            var mainYamlContent = @"
powerFxDefinitions:
  - location: './subfolder/definitions.yaml'
";
            var subfolderYamlContent = @"
powerFxTestTypes:
  - name: SubfolderType
    value: '{Id: Number}'
";
            string subfolderPath = Path.Combine(TestDirectory, "subfolder", "definitions.yaml");
            string subfolderDirectory = Path.Combine(TestDirectory, "subfolder");

            MockFileSystem.Setup(fs => fs.FileExists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(TestFilePath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(TestFilePath)).Returns(mainYamlContent);

            MockFileSystem.Setup(fs => fs.FileExists(subfolderPath)).Returns(true);
            MockFileSystem.Setup(fs => fs.Exists(subfolderPath)).Returns(true);
            MockFileSystem.Setup(fs => fs.ReadAllText(subfolderPath)).Returns(subfolderYamlContent);

            var loader = new PowerFxDefinitionLoader(MockFileSystem.Object, MockLogger.Object);
            var settings = new TestSettings();

            // Act
            loader.LoadPowerFxDefinitionsFromFile(TestFilePath, settings);

            // Assert
            Assert.Single(settings.PowerFxTestTypes);
            Assert.Equal("SubfolderType", settings.PowerFxTestTypes[0].Name);
        }
    }
}
