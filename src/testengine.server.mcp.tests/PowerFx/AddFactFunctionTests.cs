// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.PowerApps.TestEngine.MCP.PowerFx;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    public class AddFactFunctionTests
    {        [Fact]
        public async Task Execute_CreatesFactsTable_WhenTableDoesNotExist()
        {
            // Arrange
            var recalcEngine = new RecalcEngine();
            var addFactFunction = new AddFactFunction(recalcEngine);
            var factRecord = CreateFactRecord("TestKey", "TestValue");

            // Act
            var result = addFactFunction.Execute(factRecord);

            // Assert
            Assert.True(result.Value);
            
            // Verify the Facts table was created
            var factsTable = recalcEngine.Eval("Facts") as TableValue;
            Assert.NotNull(factsTable);
              // Verify table structure
            List<NamedValue> fields = new List<NamedValue>();

            await foreach (var field in factsTable.Rows.First().Value.GetFieldsAsync(CancellationToken.None))
            {
                fields.Add(field);
            }

            Assert.Equal(4, fields.Count());
            
            // Verify all expected fields are present, regardless of order
            Assert.Contains(fields, field => field.Name == "Id");
            Assert.Contains(fields, field => field.Name == "Category");
            Assert.Contains(fields, field => field.Name == "Key"); 
            Assert.Contains(fields, field => field.Name == "Value");

            // Verify the row was added
            Assert.Single(factsTable.Rows);
            Assert.Equal("TestKey", GetRowFieldValue(factsTable, 0, "Key"));
            Assert.Equal("TestValue", GetRowFieldValue(factsTable, 0, "Value"));
            Assert.Equal("General", GetRowFieldValue(factsTable, 0, "Category")); // Default category
        }

        [Fact]
        public void Execute_AddsToExistingFactsTable_WhenTableExists()
        {
            // Arrange
            var recalcEngine = new RecalcEngine();
            var addFactFunction = new AddFactFunction(recalcEngine);
            
            // Add first fact
            var factRecord1 = CreateFactRecord("Key1", "Value1");
            addFactFunction.Execute(factRecord1);
            
            // Act - add second fact
            var factRecord2 = CreateFactRecord("Key2", "Value2");
            var result = addFactFunction.Execute(factRecord2);

            // Assert
            Assert.True(result.Value);
            
            // Verify the Facts table contains both facts
            var factsTable = recalcEngine.Eval("Facts") as TableValue;
            Assert.NotNull(factsTable);
            Assert.Equal(2, factsTable.Rows.Count());
            
            // The table should maintain all previous rows
            Assert.Contains(factsTable.Rows, r => 
                (r.Value as RecordValue).GetField("Key").ToObject().ToString() == "Key1" && 
                (r.Value as RecordValue).GetField("Value").ToObject().ToString() == "Value1");
            
            // And have the new row as well
            Assert.Contains(factsTable.Rows, r => 
                (r.Value as RecordValue).GetField("Key").ToObject().ToString() == "Key2" && 
                (r.Value as RecordValue).GetField("Value").ToObject().ToString() == "Value2");
        }        [Fact]
        public void Execute_AcceptsCategory_AsSecondParameter()
        {
            // Arrange
            var recalcEngine = new RecalcEngine();
            var addFactFunction = new AddFactFunction(recalcEngine);
            var factRecord = CreateFactRecord("TestKey", "TestValue");
            var category = FormulaValue.New("TestCategory");

            // Act
            var result = addFactFunction.ExecuteWithCategory(factRecord, (StringValue)category);

            // Assert
            Assert.True(result.Value);
            
            // Verify the Facts table was created with the category
            var factsTable = recalcEngine.Eval("Facts") as TableValue;
            Assert.NotNull(factsTable);
            Assert.Single(factsTable.Rows);
            Assert.Equal("TestCategory", GetRowFieldValue(factsTable, 0, "Category"));
        }

        [Fact]
        public void Execute_HandlesComplexValues_AsJson()
        {
            // Arrange
            var recalcEngine = new RecalcEngine();
            var addFactFunction = new AddFactFunction(recalcEngine);
            
            // Create fact with a complex nested value
            var complexValueFields = new[]
            {
                new NamedValue("Type", FormulaValue.New("LoginScreen")),
                new NamedValue("ScreenName", FormulaValue.New("LoginScreen1")),
                new NamedValue("TestPriority", FormulaValue.New("High"))
            };
            
            var complexValue = RecordValue.NewRecordFromFields(complexValueFields);
            
            var factFields = new[]
            {
                new NamedValue("Key", FormulaValue.New("TestPattern")),
                new NamedValue("Value", complexValue),
                new NamedValue("Category", FormulaValue.New("TestPatterns"))
            };
            
            var factRecord = RecordValue.NewRecordFromFields(factFields);
            
            // Act
            var result = addFactFunction.Execute(factRecord);
            
            // Assert
            Assert.True(result.Value);
            
            // Verify complex value was serialized properly
            var factsTable = recalcEngine.Eval("Facts") as TableValue;
            var valueJson = GetRowFieldValue(factsTable, 0, "Value");
            
            // JSON should contain all the complex value properties
            Assert.Contains("LoginScreen", valueJson);
            Assert.Contains("TestPriority", valueJson);
            Assert.Contains("High", valueJson);
        }

        [Fact]
        public void Execute_GeneratesId_WhenIdNotProvided()
        {
            // Arrange
            var recalcEngine = new RecalcEngine();
            var addFactFunction = new AddFactFunction(recalcEngine);
            
            // Create fact record without Id
            var namedValues = new[]
            {
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            };
            var factRecord = RecordValue.NewRecordFromFields(namedValues);
            
            // Act
            var result = addFactFunction.Execute(factRecord);

            // Assert
            Assert.True(result.Value);
            
            // Verify Id was generated
            var factsTable = recalcEngine.Eval("Facts") as TableValue;
            var idValue = GetRowFieldValue(factsTable, 0, "Id");
            Assert.NotNull(idValue);
            Assert.NotEmpty(idValue);
            
            // Verify it's a valid GUID format (common for auto-generated IDs)
            Assert.True(Guid.TryParse(idValue, out _), "The generated ID should be in GUID format");
        }

        [Fact]
        public void Execute_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            var recalcEngine = new RecalcEngine();
            var addFactFunction = new AddFactFunction(recalcEngine);
            var factRecord = CreateFactRecord("TestKey", "TestValue");

            // Act
            var result = addFactFunction.Execute(factRecord);

            // Assert
            Assert.True(result.Value);
        }        private RecordValue CreateFactRecord(string key, string value, string id = null, string category = null)
        {
            var namedValuesList = new List<NamedValue>();
            
            if (id != null)
            {
                namedValuesList.Add(new NamedValue("Id", FormulaValue.New(id)));
            }
            
            namedValuesList.Add(new NamedValue("Key", FormulaValue.New(key)));
            namedValuesList.Add(new NamedValue("Value", FormulaValue.New(value)));
            
            if (category != null)
            {
                namedValuesList.Add(new NamedValue("Category", FormulaValue.New(category)));
            }
            
            return RecordValue.NewRecordFromFields(namedValuesList.ToArray());
        }

        private string? GetRowFieldValue(TableValue table, int rowIndex, string fieldName)
        {
            var row = table.Rows.ElementAt(rowIndex).Value as RecordValue;
            if (row != null) {
                var field = row.GetField(fieldName);
                
                // Check if the field exists and is of type StringValue
                if (field is StringValue stringValue)
                {
                    return stringValue.Value;
                }
                // Handle other value types by converting to string
                else if (field != null)
                {
                    return field.ToString();
                }
            }
            return null;
        }
    }
}
