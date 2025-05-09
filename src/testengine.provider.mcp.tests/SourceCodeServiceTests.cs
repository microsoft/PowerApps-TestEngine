// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Providers.Tests
{
    public class SourceCodeServiceTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<IEnvironmentVariable> _mockEnvironmentVariable;
        private readonly RecalcEngine _recalcEngine;
        private readonly SourceCodeService _sourceCodeService;

        public SourceCodeServiceTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockEnvironmentVariable = new Mock<IEnvironmentVariable>();
            _recalcEngine = new RecalcEngine();
            _sourceCodeService = new SourceCodeService(_recalcEngine);
            _sourceCodeService.FileSystemFactory = () => _mockFileSystem.Object;
            _sourceCodeService.EnvironmentVariableFactory = () => _mockEnvironmentVariable.Object;
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRecalcEngineIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SourceCodeService(null));
        }

        [Fact]
        public void LoadSolutionSourceCode_ShouldLoadFilesSuccessfully_WhenPathIsValid()
        {
            // Arrange
            var validPath = "valid/path";
            var files = new[] { "file1.json", "file2.json" };
            _mockEnvironmentVariable.Setup(m => m.GetVariable(SourceCodeService.ENVIRONMENT_SOLUTION_PATH)).Returns(validPath);
            _mockFileSystem.Setup(fs => fs.Exists(validPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.GetFiles(validPath)).Returns(files);
            
            // Act
            _sourceCodeService.LoadSolutionFromSourceControl(Guid.NewGuid().ToString(), string.Empty);

            // Assert
            _mockFileSystem.Verify(fs => fs.GetFiles(validPath), Times.Once);
            Assert.NotNull(_recalcEngine.GetValue("Files"));
        }

        [Fact]
        public void LoadSolutionSourceCode_ShouldClassifyFilesCorrectly()
        {
            // Arrange
            const string CANVAS_APP = "valid/path/canvasapps/canvasapp.yaml";
            const string ENTITY = "valid/path/entities/test/entity.yaml";
            const string FLOW = "valid/path/modernflows/sample-85DC37D4-8D2B-F011-8C4C-000D3A5A111E.json";

            var validPath = "valid/path";
            var files = new[] { CANVAS_APP, ENTITY, FLOW };
            _mockEnvironmentVariable.Setup(m => m.GetVariable(SourceCodeService.ENVIRONMENT_SOLUTION_PATH)).Returns(validPath);

            _mockFileSystem.Setup(fs => fs.Exists(validPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.GetFiles(validPath)).Returns(files);
            _mockFileSystem.Setup(fs => fs.ReadAllText(CANVAS_APP)).Returns(string.Empty);
            _mockFileSystem.Setup(fs => fs.ReadAllText(ENTITY)).Returns(string.Empty);
            _mockFileSystem.Setup(fs => fs.ReadAllText(FLOW)).Returns(string.Empty);

            // Act
            _sourceCodeService.LoadSolutionFromSourceControl(Guid.NewGuid().ToString(), string.Empty);

            // Assert
            var canvasApps = _recalcEngine.GetValue("CanvasApps") as TableValue;
            var workflows = _recalcEngine.GetValue("Workflows") as TableValue;
            var entities = _recalcEngine.GetValue("Entities") as TableValue;

            Assert.NotNull(canvasApps);
            Assert.NotNull(workflows);
            Assert.NotNull(entities);
            Assert.Single(canvasApps.Rows);
            Assert.Single(workflows.Rows);
            Assert.Single(entities.Rows);
        }

        [Fact]
        public void LoadSolutionSourceCode_ShouldHandleUnsupportedFileTypes()
        {
            // Arrange
            var validPath = "valid/path";
            _mockEnvironmentVariable.Setup(m => m.GetVariable(SourceCodeService.ENVIRONMENT_SOLUTION_PATH)).Returns(validPath);

            var files = new[] { "unsupported.exe" };
            _mockFileSystem.Setup(fs => fs.Exists(validPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.GetFiles(validPath)).Returns(files);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _sourceCodeService.LoadSolutionFromSourceControl(Guid.NewGuid().ToString(), string.Empty));
        }

        [Fact]
        public void LoadSolutionSourceCode_ShouldParseCanvasAppCorrectly()
        {
            // Arrange
            const string CANVAS_APP = "valid/path/canvasapps/canvasapp.yaml";
            var validPath = "valid/path";
            var files = new[] { CANVAS_APP };

            var canvasAppYaml = @"
CanvasApp:
  Name: craff_flightrequestapp_c1d85
  AppVersion: 2025-05-05T02:41:27.0000000Z
  Status: Ready
  CreatedByClientVersion: 3.25042.10.0
  MinClientVersion: 3.25042.10.0
  Tags: '{""primaryDeviceWidth"":""1366"",""primaryDeviceHeight"":""768"",""supportsPortrait"":""true"",""supportsLandscape"":""true"",""primaryFormFactor"":""Tablet"",""showStatusBar"":""false"",""publisherVersion"":""3.25042.10"",""minimumRequiredApiVersion"":""2.2.0"",""hasComponent"":""false"",""hasUnlockedComponent"":""false"",""isUnifiedRootApp"":""false""}'
  IsCdsUpgraded: 0
  BackgroundColor: RGBA(0,176,240,1)
  DisplayName: Flight Request App
  IntroducedVersion: 1.0
  IsCustomizable: 1
";

            _mockEnvironmentVariable.Setup(m => m.GetVariable(SourceCodeService.ENVIRONMENT_SOLUTION_PATH)).Returns(validPath);
            _mockFileSystem.Setup(fs => fs.Exists(validPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.GetFiles(validPath)).Returns(files);
            _mockFileSystem.Setup(fs => fs.ReadAllText(CANVAS_APP)).Returns(canvasAppYaml);

            // Act
            _sourceCodeService.LoadSolutionFromSourceControl(Guid.NewGuid().ToString(), string.Empty);

            // Assert
            var canvasApps = _recalcEngine.GetValue("CanvasApps") as TableValue;
            Assert.NotNull(canvasApps);
            Assert.Single(canvasApps.Rows);

            var canvasAppRecord = canvasApps.Rows.First().Value as RecordValue;
            Assert.NotNull(canvasAppRecord);

            // Verify CanvasApp properties
            Assert.Equal("craff_flightrequestapp_c1d85", canvasAppRecord.GetField("Name").ToObject());
            
            // Verify facts
            var facts = canvasAppRecord.GetField("Facts") as TableValue;
            Assert.NotNull(facts);

            var factRecords = facts.Rows.Select(row => row.Value as RecordValue).ToList();
            Assert.Contains(factRecords, fact => fact.GetField("Key").ToObject().Equals("AppVersion") && fact.GetField("Value").ToObject().Equals("2025-05-05T02:41:27.0000000Z"));
            Assert.Contains(factRecords, fact => fact.GetField("Key").ToObject().Equals("Status") && fact.GetField("Value").ToObject().Equals("Ready"));
            Assert.Contains(factRecords, fact => fact.GetField("Key").ToObject().Equals("DisplayName") && fact.GetField("Value").ToObject().Equals("Flight Request App"));
            Assert.Contains(factRecords, fact => fact.GetField("Key").ToObject().Equals("BackgroundColor") && fact.GetField("Value").ToObject().Equals("RGBA(0,176,240,1)"));
            Assert.Contains(factRecords, fact => fact.GetField("Key").ToObject().Equals("IntroducedVersion") && fact.GetField("Value").ToObject().Equals("1.0"));
            Assert.Contains(factRecords, fact => fact.GetField("Key").ToObject().Equals("IsCustomizable") && fact.GetField("Value").ToObject().Equals("1"));
        }
    }
}
