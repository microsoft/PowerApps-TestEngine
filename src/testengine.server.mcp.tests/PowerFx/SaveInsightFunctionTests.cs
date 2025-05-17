// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;
using Moq.Language.Flow;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    public class SaveInsightFunctionTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testWorkspacePath;

        public SaveInsightFunctionTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _testWorkspacePath = Path.Combine(Path.GetTempPath(), "TestWorkspace");
        }

        [Fact]
        public void Execute_SavesInsight_WhenValidInsightIsProvided()
        {
            // Arrange
            var saveInsightFunction = new ScanStateManager.SaveInsightFunction(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
            
            var insight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("TestCategory")),
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );

            // Act
            var result = saveInsightFunction.Execute(insight);

            // Assert
            Assert.True(result.Value);
            
            // Verify the insight was added to cache (without requiring file write for every single insight)
            // Note: Actual file writes happen every 10 insights in the implementation
        }

        [Fact]
        public void Execute_ReturnsFalse_WhenRequiredFieldsAreMissing()
        {
            // Arrange
            var saveInsightFunction = new ScanStateManager.SaveInsightFunction(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
            
            // Missing required Category field
            var incompleteInsight = RecordValue.NewRecordFromFields(
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );

            // Act
            var result = saveInsightFunction.Execute(incompleteInsight);

            // Assert
            Assert.False(result.Value);
        }

        [Fact]
        public void Execute_SavesComplexValue_WhenValueIsRecord()
        {
            // Arrange
            var saveInsightFunction = new ScanStateManager.SaveInsightFunction(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
            
            // Create a complex value (nested record)
            var complexValue = RecordValue.NewRecordFromFields(
                new NamedValue("Property1", FormulaValue.New("Value1")),
                new NamedValue("Property2", FormulaValue.New(42)),
                new NamedValue("Property3", FormulaValue.New(true))
            );
            
            var insight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("ComplexCategory")),
                new NamedValue("Key", FormulaValue.New("ComplexKey")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", complexValue)
            );

            // Act
            var result = saveInsightFunction.Execute(insight);

            // Assert
            Assert.True(result.Value);
        }

        [Fact]
        public void FlushInsights_SavesAllInsightsToFiles()
        {
            // Arrange - Setup the cache with test data
            var saveInsightFunction = new ScanStateManager.SaveInsightFunction(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
            
            // Add some insights to the cache
            var insight1 = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Category1")),
                new NamedValue("Key", FormulaValue.New("Key1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("Value1"))
            );
            
            var insight2 = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Category2")),
                new NamedValue("Key", FormulaValue.New("Key2")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("Value2"))
            );
            
            // Execute to populate cache
            saveInsightFunction.Execute(insight1);
            saveInsightFunction.Execute(insight2);
            
            // Now create the flush function
            var flushFunction = new ScanStateManager.FlushInsightsFunction(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
                
            var flushParams = RecordValue.NewRecordFromFields(
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp"))
            );

            // Act
            var result = flushFunction.Execute(flushParams);

            // Assert
            Assert.True(result.Value);
              // Verify file writes were called for each expected file path
            _mockFileSystem.Verify(fs => fs.WriteTextToFile(
                It.Is<string>(path => path.Contains("TestApp.msapp_Category1.scan-state.json")),
                It.IsAny<string>(),
                It.IsAny<bool>() 
            ), Times.AtLeastOnce());
            
            _mockFileSystem.Verify(fs => fs.WriteTextToFile(
                It.Is<string>(path => path.Contains("TestApp.msapp_Category2.scan-state.json")),
                It.IsAny<string>(),
                It.IsAny<bool>() 
            ), Times.AtLeastOnce());
            
            _mockFileSystem.Verify(fs => fs.WriteTextToFile(
                It.Is<string>(path => path.Contains("TestApp.msapp.test-insights.json")),
                It.IsAny<string>(),
                It.IsAny<bool>()
            ), Times.AtLeastOnce());
        }

        [Fact]
        public void FlushInsights_ReturnsTrue_EvenWhenNoInsightsExist()
        {
            // Arrange
            var flushFunction = new ScanStateManager.FlushInsightsFunction(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
                
            var flushParams = RecordValue.NewRecordFromFields(
                new NamedValue("AppPath", FormulaValue.New("EmptyApp.msapp"))
            );

            // Act - Nothing was saved yet, so this should still succeed but not write files
            var result = flushFunction.Execute(flushParams);

            // Assert
            Assert.True(result.Value);
        }
    }
}
