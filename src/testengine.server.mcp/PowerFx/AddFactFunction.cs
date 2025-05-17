// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP.PowerFx
{
    /// <summary>
    /// PowerFx function for adding facts to a Facts table.
    /// Enhanced implementation that combines AddFact and AddContext functionality.
    /// </summary>
    public class AddFactFunction : ReflectionFunction
    {
        private readonly RecalcEngine _recalcEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddFactFunction"/> class.
        /// </summary>
        /// <param name="recalcEngine">The RecalcEngine instance to store the Facts table.</param>
        public AddFactFunction(RecalcEngine recalcEngine) 
            : base(DPath.Root, "AddFact", FormulaType.Boolean, RecordType.Empty(), StringType.String)
        {
            _recalcEngine = recalcEngine ?? throw new ArgumentNullException(nameof(recalcEngine));
        }

        /// <summary>
        /// Executes the enhanced AddFact function to add a fact to the Facts table.
        /// This version supports an optional category parameter, combining the functionality
        /// of both AddFact and AddContext.
        /// </summary>
        /// <param name="fact">The record containing fact information.</param>
        /// <returns>Boolean value indicating success or failure.</returns>
        public BooleanValue Execute(RecordValue fact)
        {
            return ExecuteWithCategory(fact, null);
        }
        
        /// <summary>
        /// Executes the enhanced AddFact function with a category parameter.
        /// </summary>
        /// <param name="fact">The record containing fact information.</param>
        /// <param name="category">Optional category for the fact (replaces AddContext functionality).</param>
        /// <returns>Boolean value indicating success or failure.</returns>
        public BooleanValue ExecuteWithCategory(RecordValue fact, StringValue category)
        {
            try
            {
                // Get fact values with category support
                string factKey = GetStringValue(fact, "Key", "Unknown");
                string factCategory = category?.Value ?? GetStringValue(fact, "Category", "General");
                string id = GetStringValue(fact, "Id", Guid.NewGuid().ToString());
                
                // Handle value - could be a string or a complex record/object
                string factValue = GetValueAsString(fact);

                // Create or append to the Facts table with category
                AddToFactsTable(id, factCategory, factKey, factValue);

                return FormulaValue.New(true);
            }
            catch (Exception)
            {
                // Return false if any error occurs during fact addition
                return FormulaValue.New(false);
            }
        }

        /// <summary>
        /// Adds a fact to the Facts table. Creates the table if it doesn't exist.
        /// </summary>
        /// <param name="id">The unique identifier for the fact.</param>
        /// <param name="category">The category of the fact (combines AddContext functionality).</param>
        /// <param name="key">The key of the fact.</param>
        /// <param name="value">The value of the fact.</param>
        private void AddToFactsTable(string id, string category, string key, string value)
        {
            TableValue existingTable = null;

            // Try to retrieve the existing Facts table
            try
            {
                var formula = _recalcEngine.Eval("Facts");
                existingTable = formula as TableValue;
            }
            catch
            {
                // Table doesn't exist yet, we'll create it
            }

            if (existingTable == null)
            {
                // Create a new empty Facts table with the enhanced schema including Category
                var columns = RecordType.Empty().Add("Id", FormulaType.String)
                    .Add("Category", FormulaType.String)
                    .Add("Key", FormulaType.String)
                    .Add("Value", FormulaType.String);
                
                // Initialize the table with our schema but no rows
                _recalcEngine.UpdateVariable("Facts", FormulaValue.NewTable(columns));
                
                // Get the newly created table
                existingTable = _recalcEngine.Eval("Facts") as TableValue;
            }

            if (existingTable != null)
            {
                // Build a list of all existing rows
                var rows = new List<RecordValue>();
                foreach (var row in existingTable.Rows)
                {
                    var record = row.Value as RecordValue;
                    rows.Add(record);
                }

                // Create new fact record with Category field
                RecordValue newFact = RecordValue.NewRecordFromFields(
                    new NamedValue("Id", FormulaValue.New(id)),
                    new NamedValue("Category", FormulaValue.New(category)),
                    new NamedValue("Key", FormulaValue.New(key)),
                    new NamedValue("Value", FormulaValue.New(value))
                );
                
                // Add the new fact row
                rows.Add(newFact);
                
                // Update the table with all rows (existing + new)
                var columns = RecordType.Empty().Add("Id", FormulaType.String)
                    .Add("Category", FormulaType.String)
                    .Add("Key", FormulaType.String)
                    .Add("Value", FormulaType.String);

                var updatedTable = TableValue.NewTable(columns, rows);
                
                _recalcEngine.UpdateVariable("Facts", updatedTable);
            }
        }

        /// <summary>
        /// Gets a string value from a record field.
        /// </summary>
        private string GetStringValue(RecordValue record, string fieldName, string defaultValue = "")
        {
            try {
                var value = record.GetField(fieldName);
                if (value is StringValue strValue)
                {
                    return strValue.Value;
                }
            } catch {
                // Ignore exceptions and return default value
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Extracts the Value property from a record, handling both simple values and complex records.
        /// </summary>
        /// <param name="record">The record containing a Value field.</param>
        /// <returns>String representation of the Value field.</returns>
        private string GetValueAsString(RecordValue record)
        {
            try
            {
                var value = record.GetField("Value");
                
                // If it's a simple string value, return it directly
                if (value is StringValue strValue)
                {
                    return strValue.Value;
                }
                // If it's a record, serialize it to JSON
                else if (value is RecordValue recordValue)
                {
                    return SerializeRecordValue(recordValue);
                }
                // For other types, convert to string
                else
                {
                    return value?.ToString() ?? "";
                }
            }
            catch
            {
                // If no Value field, return the record itself as JSON
                try
                {
                    return SerializeRecordValue(record);
                }
                catch
                {
                    return "{}"; // Empty JSON object as fallback
                }
            }
        }
        
        /// <summary>
        /// Serializes a RecordValue to a JSON string.
        /// </summary>
        private string SerializeRecordValue(RecordValue record)
        {
            var dict = new Dictionary<string, object>();
            
            foreach (var fieldName in record.Type.FieldNames)
            {
                var fieldValue = record.GetField(fieldName);
                
                // Extract field value based on type
                if (fieldValue is StringValue strValue)
                {
                    dict[fieldName] = strValue.Value;
                }
                else if (fieldValue is NumberValue numValue)
                {
                    dict[fieldName] = numValue.Value;
                }
                else if (fieldValue is BooleanValue boolValue)
                {
                    dict[fieldName] = boolValue.Value;
                }
                else if (fieldValue is RecordValue nestedRecord)
                {
                    // Handle nested records recursively
                    dict[fieldName] = SerializeRecordValue(nestedRecord);
                }
                else if (fieldValue is TableValue tableValue)
                {
                    // For tables, just store a placeholder for now
                    dict[fieldName] = "[Table Data]";
                }
                else
                {
                    dict[fieldName] = fieldValue?.ToString() ?? "";
                }
            }
            
            return JsonSerializer.Serialize(dict);
        }
    }
}
