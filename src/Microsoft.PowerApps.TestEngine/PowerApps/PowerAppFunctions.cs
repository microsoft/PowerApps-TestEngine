// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    public class PowerAppFunctions : IPowerAppFunctions
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private bool IsPowerAppLoaded { get; set; } = false;
        private bool IsPlayerJsLoaded { get; set; } = false;
        private bool IsPublishedAppTestingJsLoaded { get; set; } = false;
        private string PublishedAppIframeName { get; set; } = "fullscreen-app-host";

        public PowerAppFunctions(ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState)
        {
            _testInfraFunctions = testInfraFunctions;
            _singleTestInstanceState = singleTestInstanceState;
        }

        public async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            if (string.IsNullOrEmpty(itemPath.ControlName))
            {
                throw new ArgumentNullException(nameof(itemPath.ControlName));
            }

            if (string.IsNullOrEmpty(itemPath.PropertyName))
            {
                throw new ArgumentNullException(nameof(itemPath.PropertyName));
            }
            // TODO: handle galleries and components
            var itemPathString = JsonConvert.SerializeObject(itemPath);
            var expression = $"getPropertyValue({itemPathString})";
            return await _testInfraFunctions.RunJavascriptAsync<T>(expression);
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

        private async Task WaitForAppToBeIdleAsync()
        {
            // TODO: implement timeout
            var appIsIdle = false;
            while (!appIsIdle)
            {
                appIsIdle = await CheckIfAppIsIdleAsync();
                if (!appIsIdle)
                {
                    Thread.Sleep(500);
                }
            }
        }

        public async Task<List<PowerAppControlModel>> LoadPowerAppsObjectModelAsync()
        {
            await WaitForAppToBeIdleAsync();

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
            var controlModels = new List<PowerAppControlModel>();

            if (!string.IsNullOrEmpty(controlObjectModelJsonString))
            {
                var jsObjectModel = JsonConvert.DeserializeObject<JSObjectModel>(controlObjectModelJsonString);

                if (jsObjectModel != null && jsObjectModel.Controls != null)
                {
                    foreach (var jsControlModel in jsObjectModel.Controls)
                    {
                        if (string.IsNullOrEmpty(jsControlModel.Name) || jsControlModel.Properties == null)
                        {
                            _singleTestInstanceState.GetLogger().LogDebug("Received a control with empty name or null properties");
                        }
                        else
                        {
                            var controlModel = new PowerAppControlModel(jsControlModel.Name, jsControlModel.Properties.ToList(), this);
                            controlModels.Add(controlModel);
                        }
                    }
                }
            }

            if(controlModels.Count == 0)
            {
                _singleTestInstanceState.GetLogger().LogError("No control model was found");
            }

            return controlModels;

        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath)
        {
            if (string.IsNullOrEmpty(itemPath.ControlName))
            {
                throw new ArgumentNullException(nameof(itemPath.ControlName));
            }
            // TODO: handle galleries and components
            var itemPathString = JsonConvert.SerializeObject(itemPath);
            var expression = $"select({itemPathString})";
            return await _testInfraFunctions.RunJavascriptAsync<bool>(expression);
        }
    }
}
