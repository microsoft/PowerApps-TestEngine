// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    public class PowerAppFunctions: IPowerAppFunctions
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly ITestState _testState;
        private bool IsPlayerJsLoaded { get; set; } = false;
        private bool IsPublishedAppTestingJsLoaded { get; set; } = false;
        private string PublishedAppIframeName { get; set; } = "fullscreen-app-host";
        private TypeMapping TypeMapping = new TypeMapping();

        public PowerAppFunctions(ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState, ITestState testState)
        {
            _testInfraFunctions = testInfraFunctions;
            _singleTestInstanceState = singleTestInstanceState;
            _testState = testState;
        }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            ValidateItemPath(itemPath, true);
            var itemPathString = JsonConvert.SerializeObject(itemPath);
            var expression = $"getPropertyValue({itemPathString})";
            return await _testInfraFunctions.RunJavascriptAsync<T>(expression);
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            var getProperty = GetPropertyValueFromControlAsync<T>(itemPath).GetAwaiter();

            PollingHelper.Poll(getProperty, (x) => !x.IsCompleted, null, _testState.GetTimeout());

            return getProperty.GetResult();
        }

        private string GetFilePath(string file)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var fullFilePath = Path.Combine(currentDirectory, file);
            if (File.Exists(fullFilePath))
            {
                return fullFilePath;
            }

            return Path.Combine(Directory.GetParent(currentDirectory).FullName, "Microsoft.PowerApps.TestEngine", file);
        }

        private async Task<bool> CheckIfAppIsIdleAsync()
        {
            try
            {
                if (!IsPlayerJsLoaded)
                {
                    await _testInfraFunctions.AddScriptTagAsync(GetFilePath(Path.Combine("JS", "CanvasAppSdk.js")), null);
                    IsPlayerJsLoaded = true;
                }
                var expression = "getAppStatus()";
                return (await _testInfraFunctions.RunJavascriptAsync<string>(expression)) == "Idle";
            }
            catch (Exception ex)
            {
                _singleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                IsPlayerJsLoaded = false;
                return false;
            }

        }

        public async Task<Dictionary<string, ControlRecordValue>> LoadPowerAppsObjectModelAsync()
        {
            await PollingHelper.PollAsync<bool>(false, (x) => !x, () => CheckIfAppIsIdleAsync(), _testState.GetTestSettings().Timeout);

            if (!IsPublishedAppTestingJsLoaded)
            {
                await _testInfraFunctions.AddScriptTagAsync(GetFilePath(Path.Combine("JS", "PublishedAppTesting.js")), PublishedAppIframeName);
                IsPublishedAppTestingJsLoaded = true;
            }

            // TODO: add retry logic for changes in DOM model
            // Temporary Hack
            Thread.Sleep(1000);

            var expression = "buildObjectModel().then((objectModel) => JSON.stringify(objectModel));";
            var controlObjectModelJsonString = await _testInfraFunctions.RunJavascriptAsync<string>(expression);
            var controlDictionary = new Dictionary<string, ControlRecordValue>();

            if (!string.IsNullOrEmpty(controlObjectModelJsonString))
            {
                var jsObjectModel = JsonConvert.DeserializeObject<JSObjectModel>(controlObjectModelJsonString);

                if (jsObjectModel != null && jsObjectModel.Controls != null)
                {
                    foreach (var control in jsObjectModel.Controls)
                    {
                        if (controlDictionary.ContainsKey(control.Name))
                        {
                            // Components get declared twice at the moment so prevent it from throwing.
                            _singleTestInstanceState.GetLogger().LogDebug($"Control: {control.Name} already added");
                        }
                        else
                        {
                            var controlType = new RecordType();
                            foreach (var property in control.Properties)
                            {
                                if (TypeMapping.TryGetType(property.PropertyType, out var formulaType))
                                {
                                    controlType = controlType.Add(property.PropertyName, formulaType);
                                }
                                else
                                {
                                    _singleTestInstanceState.GetLogger().LogDebug($"Control: {control.Name}, Skipping property: {property.PropertyName}, with type: {property.PropertyType}");
                                }
                            }

                            TypeMapping.AddMapping(control.Name, controlType);

                            var controlValue = new ControlRecordValue(controlType, this, control.Name);

                            controlDictionary.Add(control.Name, controlValue);
                        }
                    }
                }
            }

            if (controlDictionary.Keys.Count == 0)
            {
                _singleTestInstanceState.GetLogger().LogError("No control model was found");
            }
            return controlDictionary;

        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            ValidateItemPath(itemPath, false);
            var itemPathString = JsonConvert.SerializeObject(itemPath);
            var expression = $"select({itemPathString})";
            return await _testInfraFunctions.RunJavascriptAsync<bool>(expression);
        }

        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            Object? objectValue = null;

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
                    objectValue = ((DateValue)value).Value;
                    break;
                default:
                    throw new ArgumentException("SetProperty must be a valid type.");
            }

            ValidateItemPath(itemPath, false);
            // TODO: handle components
            var itemPathString = JsonConvert.SerializeObject(itemPath);

            var expression = $"setPropertyValue({itemPathString}, \"{objectValue}\")";
            return await _testInfraFunctions.RunJavascriptAsync<bool>(expression);
        }


        private void ValidateItemPath(ItemPath itemPath, bool requirePropertyName)
        {
            if(string.IsNullOrEmpty(itemPath.ControlName))
            {
                throw new ArgumentNullException(nameof(itemPath.ControlName));
            }

            if (requirePropertyName || itemPath.Index.HasValue)
            {
                if (string.IsNullOrEmpty(itemPath.PropertyName))
                {
                    // Property name is required on certain functions
                    // It is also required when accessing elements in a gallery, so if an index is specified, it needs to be there
                    throw new ArgumentNullException(nameof(itemPath.PropertyName));
                }
            }

            if(itemPath.ParentControl != null)
            {
                ValidateItemPath(itemPath.ParentControl, false);
            }
        }

        private async Task<int> GetItemCountAsync(ItemPath itemPath)
        {
            ValidateItemPath(itemPath, false);
            var itemPathString = JsonConvert.SerializeObject(itemPath);
            var expression = $"getItemCount({itemPathString})";
            return await _testInfraFunctions.RunJavascriptAsync<int>(expression);

        }

        public int GetItemCount(ItemPath itemPath)
        {
            var getItemCount = GetItemCountAsync(itemPath).GetAwaiter();

            PollingHelper.Poll(getItemCount, (x) => !x.IsCompleted, null, _testState.GetTimeout());

            return getItemCount.GetResult();
        }
    }
}
