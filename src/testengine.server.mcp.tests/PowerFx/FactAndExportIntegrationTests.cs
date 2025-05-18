// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    public class FactAndExportIntegrationTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly RecalcEngine _recalcEngine;
        private readonly string _testWorkspacePath;

        public FactAndExportIntegrationTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _testWorkspacePath = Path.Combine(Path.GetTempPath(), "TestWorkspace");
            _recalcEngine = new RecalcEngine();
        }
        
        [Fact]
        public void AddFactAndSaveFact_WorkTogether_ForCompleteFactManagement()
        {            // Arrange - Set up both functions            
            var addFactFunction = ScanStateManagerAccess.CreateAddFactFunction(_recalcEngine);
            var saveFactFunction = ScanStateManagerAccess.CreateSaveFactFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            var exportFactsFunction = ScanStateManagerAccess.CreateExportFactsFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);
            
            string appPath = "TestApp.msapp";

            // Create fact record explicitly
            var fact = RecordValue.NewRecordFromFields(
                new NamedValue("Key", FormulaValue.New("TestControl")),
                new NamedValue("Value", FormulaValue.New("Button1")),
                new NamedValue("Category", FormulaValue.New("Controls"))
            );

            // Execute without optional parameters
            var result1 = addFactFunction.Execute(fact);
            Assert.True(result1.Value);

            // Now save the fact through the SaveFact function
            var controlFact = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Controls")),
                new NamedValue("Key", FormulaValue.New("TestControl")),
                new NamedValue("AppPath", FormulaValue.New(appPath)),
                new NamedValue("Value", FormulaValue.New("Button1"))
            );
            
            var result2 = saveFactFunction.Execute(controlFact);
            Assert.True(result2.Value);

            // Save another fact
            var screenFact = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Screens")),
                new NamedValue("Key", FormulaValue.New("Screen1")),
                new NamedValue("AppPath", FormulaValue.New(appPath)),
                new NamedValue("Value", new Dictionary<string, object> { 
                    { "Name", "Screen1" }, 
                    { "Type", "screen" } 
                }.ToFormulaValue())
            );
            saveFactFunction.Execute(screenFact);
            // Now export the facts
            _mockFileSystem.Setup(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), true));
            
            var exportParams = RecordValue.NewRecordFromFields(
                new NamedValue("AppPath", FormulaValue.New(appPath))
            );
              var exportResult = exportFactsFunction.Execute(exportParams);
            Assert.True(exportResult.Value); 
        }
        
        [Fact]
        public void VerifyFactsTableSchema_MatchesSaveFactSchema_ForConsistency()
        {
            // This test ensures that the AddFact function and SaveFact function
            // expect the same schema, so they can be used interchangeably by users
              // Create the recalc engine and register AddFact
            var recalcEngine = new RecalcEngine();
            var addFactFunction = ScanStateManagerAccess.CreateAddFactFunction(recalcEngine);
            recalcEngine.Config.AddFunction(addFactFunction);


             // Create fact record explicitly
            var fact = RecordValue.NewRecordFromFields(
                new NamedValue("Key", FormulaValue.New("TestControl")),
                new NamedValue("Value", FormulaValue.New("Button1")),
                new NamedValue("Category", FormulaValue.New("Controls"))
            );

            // Execute without optional parameters
            var result1 = addFactFunction.Execute(fact);
            
            // Verify recalc engine has Facts table
            var tables = recalcEngine.GetTables();
            Assert.Contains("Facts", tables);
            
            // Verify the Facts table schema matches what would go into SaveFact
            var factsTable = recalcEngine.Eval("Facts").AsTable();
            var factColumns = new List<string>();
            
            if (factsTable.Rows.Any())
            {
                var firstRow = factsTable.Rows.First().Value;
                foreach (var field in firstRow.Fields)
                {
                    factColumns.Add(field.Name);
                }
            }
            
            // These are the same field names used in the SaveFact function
            // Category, Key, Value (plus AppPath for SaveFact)
            Assert.Contains("Category", factColumns);
            Assert.Contains("Key", factColumns);
            Assert.Contains("Value", factColumns);        }
    }
}
