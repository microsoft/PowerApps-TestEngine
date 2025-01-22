using System.Dynamic;
using ICSharpCode.Decompiler.DebugInfo;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace testengine.provider.mda
{
    public class ModelDrivenApplicationCanvasState
    {
        public Dictionary<string, string?> VariableState { get; set; } = new Dictionary<string, string?>();
        public Dictionary<string, string> CollectionState { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Query the state of the browser and update the Power Fx state
        /// </summary>
        /// <param name="testInfraFunctions">Test infrastructure instance to communnicate with the browser</param>
        /// <param name="e">The test step being executed</param>
        /// <returns></returns>
        public async Task UpdateRecalcEngine(ITestInfraFunctions testInfraFunctions, TestStepEventArgs e)
        {
            string variables = "{scopeVariables: [], collections: []}";

            try
            {
                variables = await testInfraFunctions.RunJavascriptAsync<string>("PowerAppsModelDrivenCanvas.getAppMagic().getLanguageRuntime().getVariableValuesJson()");
            }
            catch (Exception)
            {
                // Ignore error, could be cause by invalid provider state, will use default value
            }

            var state = JObject.Parse(variables);

            AddScopedVariables(state, e);

            AddCollections(state, e);
        }

        /// <summary>
        /// Compare changes between this instance and the other instance. 
        /// Any variable changes found in the current state will be copied to the other state
        /// </summary>
        /// <param name="originalState">The canvas state assumed to be before test step changes applied</param>
        /// <param name="newEngine">The RecalcEngine from after the test step was applied</param>
        /// <param name="originalEngine">The RecalcEngine that contains values before the test step was applied</param>
        /// <returns></returns>
        public async Task ApplyChanges(ITestInfraFunctions testInfraFunctions, ModelDrivenApplicationCanvasState originalState, RecalcEngine newEngine, RecalcEngine originalEngine)
        {
            // Scenarios
            //
            // | Existing State | Power Fx State | New State | Changed Required                                         |
            // |----------------|----------------|-----------|----------------------------------------------------------|
            // | Same           | Same           | Same      | No change required                                       |
            // | Different      | Different      | Different | Send Power Fx state to browser and update existing state |
            // | Different      |                | Different | Update Power Fx state with new state value               |


            foreach (var variable in VariableState.Keys)
            {
                if (originalState.VariableState.ContainsKey(variable))
                {
                    // Check if variable state before and after is different
                    if (VariableState[variable] != originalState.VariableState[variable])
                    {
                        HandleVariableValueChange(variable, originalState, newEngine, originalEngine);
                    }

                    var existingValue = originalEngine.GetValue(variable);
                    var existingPowerFxValueState = await ConvertToVariableState(existingValue);
                    // Check if the orginal value has been updated
                    if (existingPowerFxValueState != originalState.VariableState[variable])
                    {
                        await HandleOriginalPowerFxValueHasChanged(testInfraFunctions, variable, originalState, existingPowerFxValueState);
                    }
                }
                else
                {
                    // New value exists in the provider that does not exist in the original state
                    if (newEngine.TryGetValue(variable, out var newPowerFxVariableValue))
                    {
                        // Add the new variable and cache a copy of the variable state
                        originalEngine.UpdateVariable(variable, newPowerFxVariableValue);
                        if (originalState.VariableState.ContainsKey(variable))
                        {
                            originalState.VariableState[variable] = await originalState.ConvertToVariableState(newPowerFxVariableValue);
                        }
                        else
                        {
                            originalState.VariableState.Add(variable, await originalState.ConvertToVariableState(newPowerFxVariableValue));
                        }
                    }
                }
            }

            foreach (var collection in CollectionState.Keys)
            {
                if (originalState.CollectionState.ContainsKey(collection))
                {
                    // Check if variable state before and after is different
                    if (CollectionState[collection] != originalState.CollectionState[collection])
                    {
                        HandleCollectionValueChange(collection, originalState, newEngine, originalEngine);
                    }

                    var existingValue = originalEngine.GetValue(collection);
                    var existingPowerFxValueState = await ConvertToVariableState(existingValue);
                    // Check if the orginal value has been updated
                    if (existingPowerFxValueState != originalState.CollectionState[collection])
                    {
                        await HandleOriginalPowerFxCollectionHasChanged(testInfraFunctions, collection, originalState, existingPowerFxValueState);
                    }
                }
                else
                {
                    // New value exists in the provider that does not exist in the original state
                    if (newEngine.TryGetValue(collection, out var newPowerFxCollectionValue))
                    {
                        // Add the new collction and cache a copy of the collection state
                        originalEngine.UpdateVariable(collection, newPowerFxCollectionValue);
                        if (!originalState.CollectionState.ContainsKey(collection))
                        {
                            originalState.CollectionState.Add(collection, await originalState.ConvertToVariableState(newPowerFxCollectionValue));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the case where the value of the original Power FX is different that the varianle state
        /// </summary>
        /// <param name="variable">The varaible to be handled</param>
        /// <param name="originalState">The canvas state to be updated</param>
        /// <param name="existingPowerFxValueState">The new value to apply the original state</param>
        private async Task HandleOriginalPowerFxValueHasChanged(ITestInfraFunctions testInfraFunctions, string variable, ModelDrivenApplicationCanvasState originalState, string? existingPowerFxValueState)
        {
            // The value of the Power Fx in the target Power Fx is different that the variable state
            // We need to update the variable state of the other instance and update the browser with the new value
            // This case could occur if a Set() function was used.

            // For variable greeting

            // | Existing State | Power Fx State | Power Fx Commmand            |
            // |----------------|----------------|------------------------------|
            // | "Hello"        | "Hello"        |                              |
            // |                |                | Set(greeting, "Hello World") |
            // | "Hello"        | "Hello World"  |                              |

            // As a result of Power Fx state with new value the Existing state and change

            if (existingPowerFxValueState != null)
            {
                originalState.VariableState[variable] = existingPowerFxValueState;

                var newValue = existingPowerFxValueState;

                var needsQuotes = true;

                if (long.TryParse(newValue, out var longValue) || decimal.TryParse(newValue, out var decimalValue))
                {
                    // Assume numeric value
                    needsQuotes = false;
                }

                if (newValue.Contains("{") && newValue.Contains("}"))
                {
                    // Assume it is an object
                    needsQuotes = false;
                }

                if (needsQuotes)
                {
                    // Assume it is a string
                    newValue = $"'{newValue}'";
                }

                await testInfraFunctions.RunJavascriptAsync<string>($"PowerAppsModelDrivenCanvas.getAppMagic().getLanguageRuntime().setScopeVariableValue('1','{variable}', {newValue})");
            }
        }

        /// <summary>
        /// Handles the case where the value of the original Power FX is different that the collection state
        /// </summary>
        /// <param name="collection">The varaible to be handled</param>
        /// <param name="originalState">The canvas state to be updated</param>
        /// <param name="existingPowerFxValueState">The new value to apply the original state</param>
        private async Task HandleOriginalPowerFxCollectionHasChanged(ITestInfraFunctions testInfraFunctions, string collection, ModelDrivenApplicationCanvasState originalState, string? existingPowerFxValueState)
        {
            // As a result of Power Fx state with new value the Existing state and change
            if (existingPowerFxValueState != null)
            {
                originalState.CollectionState[collection] = existingPowerFxValueState;

                var newValue = existingPowerFxValueState;

                await testInfraFunctions.RunJavascriptAsync<string>($"PowerAppsModelDrivenCanvas.getAppMagic().getLanguageRuntime().setScopeCollectionValue('1','{collection}', {newValue})");
            }
        }

        /// <summary>
        /// Handle the case where this class instance and related Power FX has a new value. 
        /// Copy this value and Power FX state back to the original instance
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="originalState"></param>
        /// <param name="newInstance"></param>
        /// <param name="originalInstance"></param>
        private void HandleVariableValueChange(string variable, ModelDrivenApplicationCanvasState originalState, RecalcEngine newInstance, RecalcEngine originalInstance)
        {
            // Assume that the from instance is the updated Power FX value which should replace the current state
            // Example

            // | To State       | To Power Fx State | From Power Fx State | Power Fx Commmand       |
            // |----------------|-------------------|-------------------- |-------------------------|
            // | "Hello"        | "Hello"           |                     |                         |
            // |                |                   |                     | Select(ChangeGreeting() |
            // | "Hello"        | "Hello"           | "Hello World"       |

            // In this example the from Power FX is different so that value need to be applied to the toState

            originalInstance.UpdateVariable(variable, newInstance.GetValue(variable));
            // This change assumes that this instance of the Variable is correct and shoudl replace the old one
            originalState.VariableState[variable] = VariableState[variable];
        }

        /// <summary>
        /// Handle the case where this class instance and related Power FX has a new value. 
        /// Copy this value and Power FX state back to the original instance
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="originalState"></param>
        /// <param name="newInstance"></param>
        /// <param name="originalInstance"></param>
        private void HandleCollectionValueChange(string collection, ModelDrivenApplicationCanvasState originalState, RecalcEngine newInstance, RecalcEngine originalInstance)
        {
            originalInstance.UpdateVariable(collection, newInstance.GetValue(collection));
            // This change assumes that this instance of the Variable is correct and shoudl replace the old one
            originalState.CollectionState[collection] = CollectionState[collection];
        }

        /// <summary>
        /// Convert Power FX value to serialized string representation of the value
        /// </summary>
        /// <param name="formulaValue">The value to convert</param>
        /// <returns>The serialized version of the formula</returns>
        public async Task<string?> ConvertToVariableState(FormulaValue formulaValue)
        {
            if (formulaValue.TryGetPrimitiveValue(out object primimative))
            {
                return primimative?.ToString();
            }

            if (formulaValue.TryShallowCopy(out var value))
            {
                if (value is TableValue)
                {
                    var tableValue = (TableValue)value;
                    return JsonConvert.SerializeObject(await ConvertTableValueToExpandoList(tableValue));
                }
                if (value is ObjectRecordValue)
                {
                    var objectValue = (ObjectRecordValue)value;
                    if (objectValue.ToObject() is ExpandoObject expando)
                    {
                        return JsonConvert.SerializeObject(expando);
                    }
                }
            }
            return null;

        }

        private static async Task<List<ExpandoObject>> ConvertTableValueToExpandoList(TableValue tableValue)
        {
            var expandoList = new List<ExpandoObject>();

            foreach (var row in tableValue.Rows)
            {
                dynamic expando = new ExpandoObject();
                var expandoDict = (IDictionary<string, object>)expando;

                var recordValue = row.Value;

                var fields = recordValue.GetFieldsAsync(CancellationToken.None);

                await foreach (var field in fields)
                {
                    if (field.Value.TryGetPrimitiveValue(out object value))
                    {
                        expandoDict.Add(field.Name, value);
                    }
                }

                expandoList.Add(expando);
            }

            return expandoList;
        }

        private void AddCollections(JObject state, TestStepEventArgs e)
        {
            var collections = state.Property("collections").Children().FirstOrDefault();

            if (collections != null)
            {
                foreach (var collection in collections.Children())
                {
                    if (collection is JObject)
                    {
                        var collectionObject = collection as JObject;
                        foreach (JProperty property in collectionObject.Properties())
                        {
                            var collectionValue = property.Value;

                            if (collectionValue is JArray)
                            {
                                var recordType = RecordType.Empty();
                                var addType = true;

                                var records = new List<RecordValue>();
                                foreach (var item in collectionValue.Children())
                                {
                                    if (item is JObject)
                                    {
                                        var itemValue = item as JObject;
                                        var fields = new List<NamedValue>();

                                        foreach (var prop in itemValue.Properties())
                                        {
                                            if (addType)
                                            {
                                                recordType = recordType.Add(prop.Name, GetFormulaType(prop.Value.Type));
                                            }
                                            fields.Add(new NamedValue(prop.Name, GetFormulaValue(prop.Value, GetFormulaType(prop.Value.Type))));
                                        }

                                        var record = RecordValue.NewRecordFromFields(fields);
                                        records.Add(record);
                                    }
                                }

                                var table = TableValue.NewTable(recordType, records);
                                e.Engine.UpdateVariable(property.Name, table);

                                var collectionValueJson = JsonConvert.SerializeObject(ConvertJTokenToObject(collectionValue));
                                if (CollectionState.ContainsKey(property.Name))
                                {
                                    CollectionState[property.Name] = collectionValueJson;
                                }
                                else
                                {
                                    CollectionState.Add(property.Name, collectionValueJson);
                                }
                            }
                        }
                    }
                }
            }
        }

        //public static TableValue LoadJArrayIntoTableValue(JArray jsonArray)
        //{
        //    var recordType = RecordType.Empty();
        //    var tableValues = new List<RecordValue>();
        //    var addType = true;

        //    foreach (JObject jsonObject in jsonArray)
        //    {
        //        var recordValues = new List<NamedValue>();

        //        foreach (var property in jsonObject.Properties())
        //        {
        //            var fieldType = GetFormulaType(property.Value);
        //            if (addType)
        //            {
        //                recordType = recordType.Add(property.Name, fieldType);
        //            }

        //            recordValues.Add(new NamedValue(property.Name, GetFormulaValue(property.Value, fieldType)));
        //        }
        //        addType = false;

        //        tableValues.Add(RecordValue.NewRecordFromFields(recordType, recordValues));
        //    }

        //    // TODO: Add support to tables of different types
        //    return TableValue.NewTable(recordType, tableValues.ToArray());
        //}

        private static FormulaType GetFormulaType(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return FormulaType.Number;
                case JTokenType.Float:
                    return FormulaType.Decimal;
                case JTokenType.String:
                    return FormulaType.String;
                case JTokenType.Boolean:
                    return FormulaType.Boolean;
                case JTokenType.Date:
                    return FormulaType.DateTime;
                default:
                    return FormulaType.UntypedObject;
            }
        }

        private static FormulaValue GetFormulaValue(JToken token, FormulaType type)
        {
            switch (type.ToString())
            {
                case "Number":
                    return FormulaValue.New(token.Value<double>());
                case "Decimal":
                    return FormulaValue.New(token.Value<decimal>());
                case "String":
                    return FormulaValue.New(token.Value<string>());
                case "Boolean":
                    return FormulaValue.New(token.Value<bool>());
                case "DateTime":
                    return FormulaValue.New(token.Value<DateTime>());
                default:
                    return FormulaValue.NewBlank();
            }
        }

        private static object? ConvertJTokenToObject(JToken token)
        {
            // Convert JToken to JSON string
            string jsonString = token.ToString();

            if (jsonString.Contains("[") && jsonString.Contains("]"))
            {
                return JsonConvert.DeserializeObject<List<ExpandoObject>>(jsonString);
            }

            // Deserialize JSON string to System.Object
            return JsonConvert.DeserializeObject<ExpandoObject>(jsonString);
        }

        private void AddScopedVariables(JObject state, TestStepEventArgs e)
        {
            var javaScriptVariables = state.Property("scopeVariables").Children().First();

            foreach (var variable in javaScriptVariables.Children())
            {
                if (variable is JProperty)
                {
                    var property = variable as JProperty;
                    object propertyValue = null;
                    FormulaValue engineValue = null;
                    var name = property.Name;
                    if (name.StartsWith("1."))
                    {
                        name = name.Substring(2);
                    }

                    if (!VariableState.ContainsKey(name))
                    {
                        if (property.Value is JObject)
                        {
                            var scopedVariable = property.Value as JObject;
                            var scopePropetryValue = scopedVariable.Property("1");

                            // TODO JArray

                            if (scopePropetryValue.Value is JObject)
                            {
                                var recordType = RecordType.Empty();
                                var objectPropertyValue = scopePropetryValue.Value;
                                foreach (var prop in objectPropertyValue.Children())
                                {
                                    if (prop is JProperty)
                                    {
                                        var objectProperty = prop as JProperty;
                                        recordType.Add(objectProperty.Name, GetFormulaType(objectProperty.Value.Type));
                                    }
                                }
                                var recordValue = new ObjectRecordValue(objectPropertyValue, recordType);
                                engineValue = recordValue;
                                propertyValue = JsonConvert.SerializeObject(objectPropertyValue);
                            }

                            if (scopePropetryValue.Value is JValue)
                            {
                                var jsonPropertyValue = scopePropetryValue.Value as JValue;
                                var propertyValueItem = jsonPropertyValue.Value;
                                if (propertyValueItem != null)
                                {
                                    switch (jsonPropertyValue.Type)
                                    {
                                        case JTokenType.Integer:
                                            if (propertyValueItem is int)
                                            {
                                                var intValue = ((int)propertyValueItem);
                                                propertyValue = intValue;
                                                engineValue = FormulaValue.New(intValue);
                                            }
                                            if (propertyValueItem is long)
                                            {
                                                var longValue = ((long)propertyValueItem);
                                                propertyValue = longValue;
                                                engineValue = FormulaValue.New(longValue);
                                            }
                                            break;
                                        case JTokenType.Float:
                                            if (propertyValueItem is float)
                                            {
                                                var floatValue = ((float)propertyValueItem);
                                                propertyValue = floatValue;
                                                engineValue = FormulaValue.New(floatValue);
                                            }
                                            if (propertyValueItem is double)
                                            {
                                                var doubleValue = ((double)propertyValueItem);
                                                propertyValue = doubleValue;
                                                engineValue = FormulaValue.New(doubleValue);
                                            }
                                            break;
                                        case JTokenType.String:
                                            var stringValue = ((string)propertyValueItem);
                                            propertyValue = stringValue;
                                            if (IsGuid(stringValue, out var guidValue))
                                            {
                                                propertyValue = guidValue;
                                                engineValue = FormulaValue.New(guidValue);
                                            }
                                            else
                                            {
                                                engineValue = FormulaValue.New(stringValue);
                                            }
                                            break;
                                        case JTokenType.Boolean:
                                            var boolValue = ((bool)jsonPropertyValue);
                                            propertyValue = boolValue;
                                            engineValue = FormulaValue.New(boolValue);
                                            break;
                                    }
                                }
                            }
                        }

                        if (engineValue != null)
                        {
                            e.Engine.UpdateVariable(name, engineValue);
                            VariableState.Add(name, propertyValue?.ToString());
                        }
                    }
                }

            }
        }

        private FormulaType GetFormulaType(JTokenType type)
        {
            switch (type)
            {
                case JTokenType.Boolean:
                    return FormulaType.Boolean;
                case JTokenType.Integer:
                    return FormulaType.Number;
                case JTokenType.String:
                    return FormulaType.String;
            }
            return FormulaType.Unknown;
        }

        private class ObjectRecordValue : RecordValue
        {
            Dictionary<string, object>? _value;

            public ObjectRecordValue(object value, RecordType type) : base(type)
            {
                _value = ConvertObjectToDictionary(value);
            }

            public Dictionary<string, object>? ConvertObjectToDictionary(object obj)
            {
                // Convert the object to a JSON string
                string jsonString = JsonConvert.SerializeObject(obj);

                // Parse the JSON string to a JObject
                JObject jObject = JObject.Parse(jsonString);

                // Convert the JObject to a dictionary
                Dictionary<string, object> dictionary = jObject.ToObject<Dictionary<string, object>>();

                return dictionary;
            }

            protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
            {
                if (_value == null && _value.ContainsKey(fieldName))
                {
                    result = null;
                    return false;
                }

                object fieldValue;
                if (_value.TryGetValue(fieldName, out fieldValue))
                {
                    switch (fieldType.GetType().ToString())
                    {
                        case "System.String":
                            result = FormulaValue.New(fieldValue.ToString());
                            break;
                        case "System.Int":
                            result = FormulaValue.New((int)fieldValue);
                            break;
                        default:
                            result = BlankValue.NewBlank();
                            break;
                    }
                    return true;
                }

                result = BlankValue.NewBlank();
                return true;
            }

            // Override ToObject method
            public override Object ToObject()
            {
                // Convert the Dictionary to an ExpandoObject
                ExpandoObject expandoObject = new ExpandoObject();
                IDictionary<string, object> expandoDict = expandoObject as IDictionary<string, object>;

                foreach (var kvp in _value)
                {
                    expandoDict.Add(kvp);
                }

                return expandoObject;
            }
        }

        private static bool IsGuid(string input, out Guid guid)
        {
            return Guid.TryParse(input, out guid);
        }
    }
}
