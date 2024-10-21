// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class PowerAppFunctions : ITestWebProvider
    {
        public static string EmbeddedJSFolderPath = "JS";
        public static string PublishedAppIframeName = "fullscreen-app-host";
        public string CheckTestEngineObject { get; } = "typeof PowerAppsTestEngine";
        public string CheckTestEngineReadyFunction { get; } = "typeof PowerAppsTestEngine.testEngineReady";

        private string GetItemCountErrorMessage = "Something went wrong when Test Engine tried to get item count.";
        private string GetPropertyValueErrorMessage = "Something went wrong when Test Engine tried to get property value.";
        private string LoadObjectModelErrorMessage = "Something went wrong when Test Engine tried to load object model.";
        private string FileNotFoundErrorMessage = "Something went wrong when Test Engine tried to load required dependencies.";
        private TypeMapping TypeMapping = new TypeMapping();

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public ITestProviderState? ProviderState { get; set; }

        public PowerAppFunctions()
        {

        }

        public PowerAppFunctions(ITestInfraFunctions? testInfraFunctions, ISingleTestInstanceState? singleTestInstanceState, ITestState? testState)
        {
            this.TestInfraFunctions = testInfraFunctions;
            this.SingleTestInstanceState = singleTestInstanceState;
            this.TestState = testState;
        }

        public string Name { get { return "canvas"; } }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, true);
                var itemPathString = JsonConvert.SerializeObject(itemPath);
                var expression = $"PowerAppsTestEngine.getPropertyValue({itemPathString}).then((propertyValue) => JSON.stringify(propertyValue))";
                return await TestInfraFunctions.RunJavascriptAsync<T>(expression);
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

        private string GetFilePath(string file)
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            var fullFilePath = Path.Combine(assemblyDirectory, file);
            if (File.Exists(fullFilePath))
            {
                return fullFilePath;
            }
            else
            {
                throw new FileNotFoundException(FileNotFoundErrorMessage, file);
            }
        }

        public async Task<bool> CheckIsIdleAsync()
        {
            try
            {
                var expression = "PowerAppsTestEngine.getAppStatus()";
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
                var expression = "PowerAppsTestEngine.buildObjectModel().then((objectModel) => JSON.stringify(objectModel))";
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

        private async Task<string> GetPowerAppsTestEngineObject()
        {
            var result = "undefined";

            try
            {
                result = await TestInfraFunctions.RunJavascriptAsync<string>(CheckTestEngineObject);
            }
            catch (NullReferenceException) { }

            return result;
        }

        public async Task CheckProviderAsync()
        {
            try
            {
                // See if using legacy player
                try
                {
                    await PollingHelper.PollAsync<string>("undefined", (x) => x.ToLower() == "undefined", () => GetPowerAppsTestEngineObject(), TestState.GetTestSettings().Timeout, SingleTestInstanceState.GetLogger());
                }
                catch (TimeoutException)
                {
                    SingleTestInstanceState.GetLogger().LogInformation("Legacy WebPlayer in use, injecting embedded JS.");
                    await TestInfraFunctions.AddScriptTagAsync(GetFilePath(Path.Combine(EmbeddedJSFolderPath, "CanvasAppSdk.js")), null);
                    await TestInfraFunctions.AddScriptTagAsync(GetFilePath(Path.Combine(EmbeddedJSFolderPath, "PublishedAppTesting.js")), PublishedAppIframeName);
                }
            }
            catch (Exception ex)
            {
                SingleTestInstanceState.GetLogger().LogDebug(ex.ToString());
            }
        }

        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controlDictionary = new Dictionary<string, ControlRecordValue>();
            SingleTestInstanceState.GetLogger().LogDebug("Start to load power apps object model");
            await PollingHelper.PollAsync(controlDictionary, (x) => x.Keys.Count == 0, (x) => LoadObjectModelAsyncHelper(x), TestState.GetTestSettings().Timeout, SingleTestInstanceState.GetLogger(), LoadObjectModelErrorMessage);
            SingleTestInstanceState.GetLogger().LogDebug($"Finish loading. Loaded {controlDictionary.Keys.Count} controls");

            return controlDictionary;
        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, false);
                var itemPathString = JsonConvert.SerializeObject(itemPath);
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
                    case (ColorType):
                        objectValue = ((ColorValue)value).Value;
                        break;
                    default:
                        throw new ArgumentException("SetProperty must be a valid type.");
                }

                ValidateItemPath(itemPath, false);

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
                var expression = $"PowerAppsTestEngine.debugInfo";
                return await TestInfraFunctions.RunJavascriptAsync<object>(expression);
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
                // check if ready function exists in the webplayer JSSDK, older versions won't have this new function
                var checkIfReadyExists = await TestInfraFunctions.RunJavascriptAsync<string>(CheckTestEngineReadyFunction);
                if (checkIfReadyExists != "undefined")
                {
                    var expression = $"PowerAppsTestEngine.testEngineReady()";
                    return await TestInfraFunctions.RunJavascriptAsync<bool>(expression);
                }

                // To support webplayer version without ready function 
                // return true for this without interrupting the test run
                return true;
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
            var environment = TestState.GetEnvironment();
            if (string.IsNullOrEmpty(environment))
            {
                SingleTestInstanceState.GetLogger().LogError("Environment cannot be empty.");
                throw new InvalidOperationException();
            }

            var testSuiteDefinition = SingleTestInstanceState.GetTestSuiteDefinition();
            if (testSuiteDefinition == null)
            {
                SingleTestInstanceState.GetLogger().LogError("Test definition must be specified.");
                throw new InvalidOperationException();
            }

            var appLogicalName = testSuiteDefinition.AppLogicalName;
            var appId = testSuiteDefinition.AppId;

            if (string.IsNullOrEmpty(appLogicalName) && string.IsNullOrEmpty(appId))
            {
                SingleTestInstanceState.GetLogger().LogError("At least one of the App Logical Name or App Id must be defined.");
                throw new InvalidOperationException();
            }

            var tenantId = TestState.GetTenant();
            if (string.IsNullOrEmpty(tenantId))
            {
                SingleTestInstanceState.GetLogger().LogError("Tenant cannot be empty.");
                throw new InvalidOperationException();
            }

            var queryParametersForTestUrl = GetQueryParametersForTestUrl(tenantId, additionalQueryParams);

            return !string.IsNullOrEmpty(appLogicalName) ?
                   $"https://{domain}/play/e/{environment}/an/{appLogicalName}{queryParametersForTestUrl}" :
                   $"https://{domain}/play/e/{environment}/a/{appId}{queryParametersForTestUrl}";
        }

        private static string GetQueryParametersForTestUrl(string tenantId, string additionalQueryParams)
        {
            return $"?tenantId={tenantId}&source=testengine{additionalQueryParams}";
        }
    }
}
