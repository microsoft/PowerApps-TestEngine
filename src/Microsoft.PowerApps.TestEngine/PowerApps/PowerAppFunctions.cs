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
        private string PublishedAppIframeName { get; set; } = "";

        public PowerAppFunctions(ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState)
        {
            _testInfraFunctions = testInfraFunctions;
            _singleTestInstanceState = singleTestInstanceState;
        }

        public async Task<T> GetPropertyValueFromControlAsync<T>(string controlName, string propertyName, string? parentControlName, int? rowOrColumnNumber)
        {
            String expression = $"getPropertyValueFromControl(\"{controlName}\", \"{propertyName}\", \"{parentControlName}\", {rowOrColumnNumber})";
            return await _testInfraFunctions.RunJavascriptAsync<T>(expression, PublishedAppIframeName);
        }

        private string GetFilePath(string file)
        {
            var fullFilePath = $"{Directory.GetCurrentDirectory()}\\{file}";
            if (File.Exists(fullFilePath))
            {
                return fullFilePath;
            }

            return $"{Directory.GetCurrentDirectory()}\\..\\Microsoft.PowerApps.TestEngine\\{file}";
        }

        private async Task<bool> CheckIfAppIsIdleAsync()
        {
            try
            {
                if (!IsPublishedAppTestingJsLoaded)
                {
                    await _testInfraFunctions.AddScriptTagAsync(GetFilePath(@"\JS\PublishedAppTesting.js"), PublishedAppIframeName);
                    IsPublishedAppTestingJsLoaded = true;
                }
                var expression = "isAppIdle()";
                return await _testInfraFunctions.RunJavascriptAsync<bool>(expression, PublishedAppIframeName);
            }
            catch (Exception ex)
            {
                _singleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                IsPublishedAppTestingJsLoaded = false;
                return false;
            }

        }

        private async Task WaitForAppToBeIdleAsync()
        {
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
            await WaitForPowerAppToLoadAsync();
            await WaitForAppToBeIdleAsync();

            // TODO: add retry logic for changes in DOM model
            // Temporary Hack
            Thread.Sleep(1000);

            var expression = "JSON.stringify(buildControlObjectModel());";
            var controlObjectModelJsonString = await _testInfraFunctions.RunJavascriptAsync<string>(expression, PublishedAppIframeName);

            var jsControlModels = JsonConvert.DeserializeObject<List<JSControlModel>>(controlObjectModelJsonString);
            var controlModels = new List<PowerAppControlModel>();

            if (jsControlModels != null)
            {
                foreach (var jsControlModel in jsControlModels)
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
            } else
            {
                _singleTestInstanceState.GetLogger().LogError("No control model was found");
            }

            return controlModels;

        }

        public async Task<bool> SelectControlAsync(string controlName, string? parentControlName, int? rowOrColumnNumber)
        {
            var expression = $"selectControl(\"{controlName}\", \"{parentControlName}\", {rowOrColumnNumber})";
            return await _testInfraFunctions.RunJavascriptAsync<bool>(expression, PublishedAppIframeName);
        }

        public async Task<bool> CheckIfAppIsLoadingAsync()
        {
            try
            {
                if (!IsPlayerJsLoaded)
                {
                    await _testInfraFunctions.AddScriptTagAsync(GetFilePath(@"\JS\PlayerTesting.js"), null);
                    IsPlayerJsLoaded = true;
                }
                var expression = "checkIfAppIsLoading()";
                return await _testInfraFunctions.RunJavascriptAsync<bool>(expression, null);
            }
            catch (Exception ex)
            {
                _singleTestInstanceState.GetLogger().LogDebug(ex.ToString());
                IsPlayerJsLoaded = false;
                return true;
            }
        }

        public async Task WaitForPowerAppToLoadAsync()
        {
            while (!IsPowerAppLoaded)
            {
                var appIsLoading = await CheckIfAppIsLoadingAsync();
                if (!appIsLoading)
                {
                    String expression = "getPublishedAppIframeName()";
                    PublishedAppIframeName = await _testInfraFunctions.RunJavascriptAsync<string>(expression, null);
                    IsPowerAppLoaded = true;
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }
    }
}
