// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using System.Reflection;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Functions for interacting with a Power Apps Model Driven Application (MDA)
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class ModelDrivenApplicationProvider : ITestWebProvider
    {
        private TypeMapping TypeMapping = new TypeMapping();

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public ITestProviderState? ProviderState { get; set; }

        public ILogger? Logger { get; set; }

        public static string QueryFormField = "JSON.stringify({{PropertyValue: PowerAppsTestEngine.getValue('{0}') }})";

        public static string ControlPropertiesQuery = "PowerAppsTestEngine.getControlProperties('{0}')";

        private string GetItemCountErrorMessage = "Something went wrong when Test Engine tried to get item count.";
        private string GetPropertyValueErrorMessage = "Something went wrong when Test Engine tried to get property value.";
        private string LoadObjectModelErrorMessage = "Something went wrong when Test Engine tried to load object model.";
        private string LoadPowerAppsMDAErrorMessage = "Something went wrong when Test Engine tried to load Power Apps Model Driven Application helper.";

        public ModelDrivenApplicationProvider()
        {

        }

        public ModelDrivenApplicationProvider(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
            this.Logger = singleTestInstanceState.GetLogger();
        }

        public string Name { get { return "mda"; } }

        public string CheckTestEngineObject
        {
            get
            {
                // TODO
                return String.Empty;
            }
        }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, true);
                var itemPathString = JsonConvert.SerializeObject(itemPath); 

                // TODO Handle other property types

                switch (itemPath.PropertyName.ToLower())
                {
                    case "text":
                        var expression = string.Format(QueryFormField, itemPath.ControlName);
                        var getValue = () => TestInfraFunctions.RunJavascriptAsync<string>(expression).Result;

                        var result = PollingHelper.Poll<string>(null, x => x == null, getValue, TestState.GetTimeout(), SingleTestInstanceState.GetLogger(), GetPropertyValueErrorMessage);

                        return (T)((object)result);
                    default:
                        var controlExpression = string.Format(ControlPropertiesQuery, itemPath.ControlName);
                        var propertiesString = await TestInfraFunctions.RunJavascriptAsync<string>(controlExpression);
                        propertiesString = propertiesString.Replace("Value: False", "Value: false");
                        propertiesString = propertiesString.Replace("Value: True", "Value: true");
                        var nameValues = JsonConvert.DeserializeObject<List<KeyValuePair<string, object>>>(propertiesString);
                        if (nameValues.Any(k => k.Key == itemPath.PropertyName))
                        {
                            var value = nameValues.First(nv => nv.Key == itemPath.PropertyName).Value;
                            switch (itemPath.PropertyName.ToLower()) {
                                case "disabled":
                                case "visible":
                                    return (T)(object)("{PropertyValue: " +  value.ToString().ToLower() + "}");
                                default:
                                    switch (value.GetType().ToString())
                                    {
                                        case "System.String":
                                            return (T)(object)("{PropertyValue: '" + value.ToString() + "'}");
                                        default:
                                            return (T)(object)("{PropertyValue: " + value.ToString() + "}");
                                    }
                            }

                            
                        }
                        break;
                }
                throw new Exception($"Unexpected property {itemPathString}");
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            var getProperty = GetPropertyValueFromControlAsync<T>(itemPath).GetAwaiter();

            PollingHelper.Poll(getProperty, (x) => !x.IsCompleted, null, TestState.GetTimeout(), SingleTestInstanceState.GetLogger(), GetPropertyValueErrorMessage);

            return getProperty.GetResult();
        }

        public async Task<bool> CheckIsIdleAsync()
        {
            try
            {
                var expression = "UCWorkBlockTracker?.isAppIdle() ? 'Idle' : 'Loading'";
                return (await TestInfraFunctions.RunJavascriptAsync<string>(expression)) == "Idle";
            }
            catch (Exception ex)
            {
                if (ex.Message?.ToString() == ExceptionHandlingHelper.PublishedAppWithoutJSSDKErrorCode)
                {
                    ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                    throw;
                }

                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                return false;
            }
        }

        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controlDictionary)
        {
            try
            {
                var expression = @"PowerAppsTestEngine.buildControlObjectModel()";
                var controlObjectModelJsonString = await TestInfraFunctions.RunJavascriptAsync<string>(expression);
                if (!string.IsNullOrEmpty(controlObjectModelJsonString))
                {
                    var jsObjectModel = JsonConvert.DeserializeObject<JSObjectModel>(controlObjectModelJsonString);

                    if (jsObjectModel != null && jsObjectModel.Controls != null)
                    {
                        SingleTestInstanceState.GetLogger().LogTrace("Listing all skipped properties for each control.");

                        foreach (var control in jsObjectModel.Controls)
                        {
                            if (controlDictionary.ContainsKey(control.Name))
                            {
                                // Components get declared twice at the moment so prevent it from throwing.
                                SingleTestInstanceState.GetLogger().LogTrace($"Control: {control.Name} already added");
                            }
                            else
                            {
                                var controlType = RecordType.Empty();
                                var skipMessage = $"Control: {control.Name}";
                                bool everSkipped = false;

                                foreach (var property in control.Properties)
                                {
                                    if (TypeMapping.TryGetType(property.PropertyType, out var formulaType))
                                    {
                                        controlType = controlType.Add(property.PropertyName, formulaType);
                                    }
                                    else
                                    {
                                        everSkipped = true;
                                        skipMessage += $"\nProperty: {property.PropertyName}, of type: {property.PropertyType}";
                                    }
                                }

                                if (everSkipped)
                                {
                                    SingleTestInstanceState.GetLogger().LogTrace(skipMessage);
                                }

                                TypeMapping.AddMapping(control.Name, controlType);

                                var controlValue = new ControlRecordValue(controlType, this, control.Name);

                                controlDictionary.Add(control.Name, controlValue);
                            }
                        }
                    }
                }

                return controlDictionary;
            }

            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public async Task CheckProviderAsync()
        {
            var pages = TestInfraFunctions.GetContext().Pages;
            if (pages.Count() > 0)
            {
                await pages.First().CloseAsync();
                TestInfraFunctions.Page = TestInfraFunctions.GetContext().Pages.First();
            }
            
            var resourceName = "testengine.provider.mda.PowerAppsTestEngineMDA.js";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                await TestInfraFunctions.AddScriptContentAsync(await reader.ReadToEndAsync());
            }

            SingleTestInstanceState.GetLogger().LogDebug("Start to load PowerAppsTestEngine");

            await PollingHelper.PollAsync(
                true,
                (x) => x == true,
                async (x) => {
                    return await TestInfraFunctions.RunJavascriptAsync<bool>("typeof PowerAppsTestEngine === 'undefined'");
                },
                TestState.GetTimeout(),
                SingleTestInstanceState.GetLogger(),
                LoadPowerAppsMDAErrorMessage);

            SingleTestInstanceState.GetLogger().LogDebug($"Finish loading PowerAppsTestEngine.");
        }

        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();
            SingleTestInstanceState.GetLogger().LogDebug("Start to load power apps object model");
            await PollingHelper.PollAsync(controlDictionary, (x) => x.Keys.Count == 0, (x) => LoadObjectModelAsyncHelper(x), TestState.GetTimeout(), SingleTestInstanceState.GetLogger(), LoadObjectModelErrorMessage);
            SingleTestInstanceState.GetLogger().LogDebug($"Finish loading. Loaded {controlDictionary.Keys.Count} controls");

            return controlDictionary;
        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, false);
                var itemPathString = JsonConvert.SerializeObject(itemPath);
                // TODO Select a choice item
                var expression = $"PowerAppsTestEngine.select({itemPathString})";
                return await TestInfraFunctions.RunJavascriptAsync<bool>(expression);
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            try
            {
                Object objectValue = null;

                switch (value.Type)
                {
                    case (NumberType):
                        objectValue = ((NumberValue)value).Value;
                        break;
                    case (StringType):
                        objectValue = ((StringValue)value).Value;
                        break;
                    case (BooleanType):
                        objectValue = ((BooleanValue)value).Value;
                        break;
                    case (DateType):
                        return await SetPropertyDateAsync(itemPath, (DateValue)value);
                    case (RecordType):
                        return await SetPropertyRecordAsync(itemPath, (RecordValue)value);
                    case (TableType):
                        return await SetPropertyTableAsync(itemPath, (TableValue)value);
                    default:
                        throw new ArgumentException("SetProperty must be a valid type.");
                }

                ValidateItemPath(itemPath, false);

                // TODO - Set the Xrm SDK Value and update state for any JS to run
                var expression = $"PowerAppsTestEngine.setPropertyValue({JsonConvert.SerializeObject(itemPath)}, {JsonConvert.SerializeObject(objectValue)})";
                return await TestInfraFunctions.RunJavascriptAsync<bool>(expression);
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public async Task<bool> SetPropertyDateAsync(ItemPath itemPath, DateValue value)
        {
            try
            {
                ValidateItemPath(itemPath, false);

                var itemPathString = JsonConvert.SerializeObject(itemPath);
                var propertyNameString = JsonConvert.SerializeObject(itemPath.PropertyName);
                var recordValue = value.GetConvertedValue(null);

                // TODO - Set the Xrm SDK Value and update state for any JS to run

                // Date.parse() parses the date to unix timestamp
                var expression = $"PowerAppsTestEngine.setPropertyValue({itemPathString},{{{propertyNameString}:Date.parse(\"{recordValue}\")}})";

                return await TestInfraFunctions.RunJavascriptAsync<bool>(expression);
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public async Task<bool> SetPropertyRecordAsync(ItemPath itemPath, RecordValue value)
        {
            try
            {
                ValidateItemPath(itemPath, false);

                var itemPathString = JsonConvert.SerializeObject(itemPath);
                var propertyNameString = JsonConvert.SerializeObject(itemPath.PropertyName);
                var recordValue = value.GetField("Value");
                var val = recordValue.GetType().GetProperty("Value").GetValue(recordValue).ToString();
                RecordValueObject json = new RecordValueObject(val);
                var checkVal = JsonConvert.SerializeObject(json);

                // TODO - Set the Xrm SDK Value and update state for any JS to run

                var expression = $"PowerAppsTestEngine.setPropertyValue({itemPathString},{{{propertyNameString}:{checkVal}}})";

                return await TestInfraFunctions.RunJavascriptAsync<bool>(expression);
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public async Task<bool> SetPropertyTableAsync(ItemPath itemPath, TableValue tableValue)
        {
            try
            {
                ValidateItemPath(itemPath, false);

                var itemPathString = JsonConvert.SerializeObject(itemPath);
                var propertyNameString = JsonConvert.SerializeObject(itemPath.PropertyName);
                RecordValueObject[] jsonArr = new RecordValueObject[tableValue.Rows.Count()];

                var index = 0;

                foreach (var row in tableValue.Rows)
                {
                    if (row.IsValue)
                    {
                        var recordValue = row.Value.Fields.First().Value;
                        var val = recordValue.GetType().GetProperty("Value").GetValue(recordValue).ToString();
                        if (!String.IsNullOrEmpty(val))
                        {
                            jsonArr[index++] = new RecordValueObject(val);
                        }
                    }
                }
                var checkVal = JsonConvert.SerializeObject(jsonArr);

                // TODO - Set the Xrm SDK Value and update state for any JS to run
                var expression = $"PowerAppsTestEngine.setPropertyValue({itemPathString},{{{propertyNameString}:{checkVal}}})";

                return await TestInfraFunctions.RunJavascriptAsync<bool>(expression);
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        private void ValidateItemPath(ItemPath itemPath, bool requirePropertyName)
        {
            if (string.IsNullOrEmpty(itemPath.ControlName))
            {
                SingleTestInstanceState.GetLogger().LogTrace("ItemPath's ControlName: " + nameof(itemPath.ControlName));
                SingleTestInstanceState.GetLogger().LogError("ItemPath's ControlName has a null value.");
                throw new ArgumentNullException();
            }

            if (requirePropertyName || itemPath.Index.HasValue)
            {
                if (string.IsNullOrEmpty(itemPath.PropertyName))
                {
                    // Property name is required on certain functions
                    // It is also required when accessing elements in a gallery, so if an index is specified, it needs to be there
                    SingleTestInstanceState.GetLogger().LogTrace("ItemPath's PropertyName: '" + nameof(itemPath.PropertyName));
                    SingleTestInstanceState.GetLogger().LogError("ItemPath's PropertyName has a null value.");
                    throw new ArgumentNullException();
                }
            }

            if (itemPath.ParentControl != null)
            {
                ValidateItemPath(itemPath.ParentControl, false);
            }
        }

        private async Task<int> GetItemCountAsync(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, false);
                var itemPathString = JsonConvert.SerializeObject(itemPath);
                var expression = $"PowerAppsTestEngine.getItemCount({itemPathString})";
                return await TestInfraFunctions.RunJavascriptAsync<int>(expression);
            }
            catch (Exception ex)
            {
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, SingleTestInstanceState.GetLogger());
                throw;
            }
        }

        public int GetItemCount(ItemPath itemPath)
        {
            var getItemCount = GetItemCountAsync(itemPath).GetAwaiter();

            PollingHelper.Poll(getItemCount, (x) => !x.IsCompleted, null, TestState.GetTimeout(), SingleTestInstanceState.GetLogger(), GetItemCountErrorMessage);

            return getItemCount.GetResult();
        }

        public async Task<object> GetDebugInfo()
        {
            try
            {
                dynamic debugInfo = new ExpandoObject();

                debugInfo.PageCount = TestInfraFunctions.GetContext().Pages.Count;
                debugInfo.PowerAppsTestEngineLoaded = await TestInfraFunctions.RunJavascriptAsync<bool>("typeof PowerAppsTestEngine !== 'undefined'");

                return debugInfo;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> TestEngineReady()
        {
            try
            {
                // TODO Inject any common JavaScript

                var isIdle = await CheckIsIdleAsync();

                if (!isIdle)
                {
                    return false;
                }

                var helperDefined = await TestInfraFunctions.RunJavascriptAsync<bool>("typeof PowerAppsTestEngine === 'undefined'");

                if ( !helperDefined )
                {
                    var resourceName = "testengine.provider.mda.PowerAppsTestEngineMDA.js";
                    var assembly = Assembly.GetExecutingAssembly();
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        await TestInfraFunctions.RunJavascriptAsync<object>(await reader.ReadToEndAsync());
                    }
                }

                return await TestInfraFunctions.RunJavascriptAsync<bool>("typeof PowerAppsTestEngine === 'undefined'");
            }
            catch (Exception ex)
            {
                // To support old apps without ready function, if the error returned is function not exists in published app
                // then return true for this without interrupting the test run
                if (ex.Message?.ToString() == ExceptionHandlingHelper.PublishedAppWithoutJSSDKErrorCode)
                {
                    return true;
                }

                // If the error returned is anything other than PublishedAppWithoutJSSDKErrorCode capture that and throw
                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                throw;
            }
        }

        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            // TODO - Construct the url
            return domain;
        }

        private static string GetQueryParametersForTestUrl(string tenantId, string additionalQueryParams)
        {
            return $"?tenantId={tenantId}&source=testengine{additionalQueryParams}";
        }
    }
}
