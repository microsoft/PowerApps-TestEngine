// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    /// <summary>
    /// Extension methods for PowerFx types to help with testing
    /// </summary>
    public static class PowerFxExtensions
    {
        /// <summary>
        /// Gets variable names from a RecalcEngine
        /// </summary>
        /// <param name="engine">The RecalcEngine instance</param>
        /// <returns>A list of variable names</returns>
        public static IEnumerable<string> GetVariableNames(this RecalcEngine engine)
        {
            // Use reflection to access the private members since this varies by PowerFx version
            try
            {
                // Try to access the property through the Config
                var variables = engine.Config.SymbolTable.SymbolNames
                    .Select(s => s.Name.Value)
                    .ToList();

                variables.AddRange(
                    engine.EngineSymbols.SymbolNames
                    .Select(s => s.Name.Value)
                    .Where(es => !variables.Any(v => v == es))
                );

                return variables;
            }
            catch
            {
                // Fallback to empty list if the method is not available
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Gets all table names from a RecalcEngine
        /// </summary>
        /// <param name="engine">The RecalcEngine instance</param>
        /// <returns>A list of table names</returns>        
        public static IEnumerable<string> GetTables(this RecalcEngine engine)
        {
            // Get variables that are tables
            var variables = engine.GetVariableNames();
            var tableNames = new List<string>();
            
            foreach (var name in variables)
            {
                try
                {
                    var value = engine.Eval(name);
                    if (value.Type is TableType)
                    {
                        tableNames.Add(name);
                    }
                }
                catch
                {
                    // Skip variables that can't be evaluated
                }
            }
            
            return tableNames;
        }
        
        /// <summary>
        /// Extension method to provide compatibility with existing code
        /// that expects a GetGlobalNames method
        /// </summary>
        /// <param name="engine">The RecalcEngine instance</param>
        /// <returns>A list of variable names</returns>
        public static IEnumerable<string> GetGlobalNames(this RecalcEngine engine)
        {
            return engine.GetVariableNames();
        }

        /// <summary>
        /// Converts a FormulaValue to a TableValue if possible
        /// </summary>
        /// <param name="value">The formula value to convert</param>
        /// <returns>The value as a TableValue</returns>
        /// <exception cref="InvalidOperationException">Thrown if the value is not a table</exception>
        public static TableValue AsTable(this FormulaValue value)
        {
            if (value is TableValue tableValue)
            {
                return tableValue;
            }
            
            throw new InvalidOperationException($"Cannot convert {value.GetType().Name} to TableValue");
        }

        /// <summary>
        /// Converts a Dictionary<string, object> to a FormulaValue
        /// </summary>
        /// <param name="dict">The dictionary to convert</param>
        /// <returns>A FormulaValue representing the dictionary</returns>
        public static FormulaValue ToFormulaValue(this Dictionary<string, object> dict)
        {
            var fields = new List<NamedValue>();
            
            foreach (var kvp in dict)
            {
                fields.Add(new NamedValue(kvp.Key, ConvertToFormulaValue(kvp.Value)));
            }
            
            return RecordValue.NewRecordFromFields(fields.ToArray());
        }

        /// <summary>
        /// Helper method to convert C# objects to FormulaValue
        /// </summary>
        private static FormulaValue ConvertToFormulaValue(object value)
        {
            if (value == null)
            {
                return FormulaValue.NewBlank();
            }
            else if (value is string stringValue)
            {
                return FormulaValue.New(stringValue);
            }
            else if (value is int intValue)
            {
                return FormulaValue.New(intValue);
            }
            else if (value is double doubleValue)
            {
                return FormulaValue.New(doubleValue);
            }
            else if (value is bool boolValue)
            {
                return FormulaValue.New(boolValue);
            }
            else if (value is Dictionary<string, object> dictValue)
            {
                return dictValue.ToFormulaValue();
            }
            else if (value is IEnumerable<object> listValue)
            {
                // Convert to a table
                var rows = listValue.Select(item => 
                    item is Dictionary<string, object> dict 
                        ? dict.ToFormulaValue() as RecordValue 
                        : RecordValue.NewRecordFromFields(new NamedValue("Value", ConvertToFormulaValue(item)))
                ).ToArray();

                if (rows.Length > 0)
                {
                    return TableValue.NewTable(rows[0].Type, rows);
                }
                else
                {
                    // Empty table
                    return TableValue.NewTable(RecordType.Empty().Add("Value", FormulaType.String));
                }
            }
            else
            {
                // Default to string representation
                return FormulaValue.New(value.ToString());
            }
        }
    }
}
