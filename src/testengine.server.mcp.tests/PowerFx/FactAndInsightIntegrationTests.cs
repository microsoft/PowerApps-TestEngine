// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class FactAndInsightIntegrationTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testWorkspacePath;
        private readonly RecalcEngine _recalcEngine;

        public FactAndInsightIntegrationTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _testWorkspacePath = Path.Combine(Path.GetTempPath(), "TestWorkspace");
            _recalcEngine = new RecalcEngine();
        }

        [Fact]
        public void AddFactAndSaveInsight_WorkTogether_ForCompleteInsightManagement()
        {
            // Arrange - Set up both functions
            var addFactFunction = new AddFactFunction(_recalcEngine);
            var saveInsightWrapper = new SaveInsightWrapper(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);

            string appPath = "TestApp.msapp";
                
            // Add a fact to the in-memory Facts table
            var fact = RecordValue.NewRecordFromFields(
                new NamedValue("Key", FormulaValue.New("TestControl")),
                new NamedValue("Value", FormulaValue.New("Button1")),
                new NamedValue("Category", FormulaValue.New("Controls"))
            );
            
            var result1 = addFactFunction.Execute(fact);
            Assert.True(result1.Value);
            
            // Verify the fact is in the Facts table
            var factsTable = _recalcEngine.Eval("Facts") as TableValue;
            Assert.NotNull(factsTable);
            Assert.Single(factsTable.Rows);
            
            // Save the same fact as an insight to disk
            var insight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Controls")),
                new NamedValue("Key", FormulaValue.New("TestControl")),
                new NamedValue("Value", FormulaValue.New("Button1")),
                new NamedValue("AppPath", FormulaValue.New(appPath))
            );
            
            var result2 = saveInsightWrapper.Execute(insight);
            Assert.True(result2.Value);
            
            // Add another fact and insight
            var fact2 = RecordValue.NewRecordFromFields(
                new NamedValue("Key", FormulaValue.New("Screen1")),
                new NamedValue("Value", FormulaValue.New("Main Screen")),
                new NamedValue("Category", FormulaValue.New("Screens"))
            );
            
            addFactFunction.Execute(fact2);
            
            var insight2 = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Screens")),
                new NamedValue("Key", FormulaValue.New("Screen1")),
                new NamedValue("Value", FormulaValue.New("Main Screen")),
                new NamedValue("AppPath", FormulaValue.New(appPath))
            );
            
            saveInsightWrapper.Execute(insight2);
            
            // Flush all insights to disk
            var flushResult = saveInsightWrapper.Flush(appPath);
            Assert.True(flushResult.Value);
            
            // Verify the Facts table has both facts
            factsTable = _recalcEngine.Eval("Facts") as TableValue;
            Assert.Equal(2, factsTable.Rows.Count());            // Verify files were written for insights            // Use Capture.In() to avoid expression tree issues with optional parameters            // Using direct string verification instead of capture, which can cause NullReferenceException
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.msapp_Controls.scan-state.json")),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)),
                Times.AtLeastOnce());
                
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.msapp_Screens.scan-state.json")),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)),
                Times.AtLeastOnce());
                
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.msapp.test-insights.json")),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)),
                Times.AtLeastOnce());
        }

        [Fact]
        public void VerifyFactsTableSchema_MatchesSaveInsightSchema_ForConsistency()
        {
            // Arrange
            var addFactFunction = new AddFactFunction(_recalcEngine);
            
            // Add a fact to create the Facts table
            var fact = RecordValue.NewRecordFromFields(
                new NamedValue("Key", FormulaValue.New("Test")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );
            
            addFactFunction.Execute(fact);
            
            // Get the Facts table and check its schema
            var factsTable = _recalcEngine.Eval("Facts") as TableValue;
            Assert.NotNull(factsTable);
            
            // Get the first row to check its fields
            var row = factsTable.Rows.First().Value as RecordValue;
              // Verify the Facts table schema matches what would go into SaveInsight
            Assert.Contains("Id", row.Type.FieldNames);
            Assert.Contains("Category", row.Type.FieldNames);
            Assert.Contains("Key", row.Type.FieldNames);
            Assert.Contains("Value", row.Type.FieldNames);
            
            // These are the same field names used in the SaveInsight function
            // Category, Key, Value (plus AppPath for SaveInsight)
            Assert.Equal(4, row.Type.FieldNames.Count());
        }
    }
}
