// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP;
using Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    public class SaveFactFunctionTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testWorkspacePath;        public SaveFactFunctionTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _testWorkspacePath = Path.Combine(Path.GetTempPath(), "TestWorkspace");
        }
        
        [Fact]
        public void Execute_SavesFact_ReturnsTrue()
        {            
            // Arrange
            var saveFactFunction = ScanStateManagerAccess.CreateSaveFactFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            var fact = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("TestCategory")),
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );
            
            // Act
            var result = saveFactFunction.Execute(fact);

            // Assert
            Assert.True(result.Value);
        }        
        
        [Fact]
        public void Execute_WithIncompleteFact_ReturnsFalse()
        {            
            // Arrange
            var saveFactFunction = ScanStateManagerAccess.CreateSaveFactFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            var incompleteFact = RecordValue.NewRecordFromFields(
                // Missing required fields
                new NamedValue("Category", FormulaValue.New("TestCategory"))
            );

            // Act
            var result = saveFactFunction.Execute(incompleteFact);

            // Assert
            Assert.False(result.Value);
        }

        [Fact]
        public void Export_CreatesFactsFile_ReturnsTrue()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), true));
            
            var exportFactsFunction = ScanStateManagerAccess.CreateExportFactsFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            // First add a few facts
            var saveFactFunction = ScanStateManagerAccess.CreateSaveFactFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);
                
            var fact1 = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Screens")),
                new NamedValue("Key", FormulaValue.New("Screen1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", new Dictionary<string, object> { 
                    { "Name", "Screen1" }, 
                    { "Type", "screen" } 
                }.ToFormulaValue())
            );

            var fact2 = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Controls")),
                new NamedValue("Key", FormulaValue.New("Button1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", new Dictionary<string, object> { 
                    { "Name", "Button1" }, 
                    { "Type", "button" } 
                }.ToFormulaValue())
            );

            saveFactFunction.Execute(fact1);
            saveFactFunction.Execute(fact2);

            var exportParams = RecordValue.NewRecordFromFields(
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp"))
            );
            
            // Act
            var result = exportFactsFunction.Execute(exportParams);
            
            // Assert
            Assert.True(result.Value);
        }
    }
}
