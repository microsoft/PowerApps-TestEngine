// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Functions for interacting with the a web based resource to test
    /// </summary>
    public interface ITestWebProvider
    {
        #nullable enable
        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }
        #nullable disable

        /// <summary>
        /// The name of the provider
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Verify if provider is usable
        /// </summary>
        public Task CheckProviderAsync();

        public string CheckTestEngineObject { get; }

        /// <summary>
        /// Generates url for the test
        /// </summary>
        /// <returns>Test url</returns>
        public string GenerateTestUrl(string domain, string queryParams);

        /// <summary>
        /// Gets the value of a property from a control.
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="itemPath">Path to the item</param>
        /// <returns>Property value</returns>
        public T GetPropertyValueFromControl<T>(ItemPath itemPath);

        /// <summary>
        /// Runs the onSelect function of a control
        /// </summary>
        /// <param name="itemPath">Path to the item</param>
        /// <returns>True if onSelect function was successfully executed.</returns>
        public Task<bool> SelectControlAsync(ItemPath itemPath);

        /// <summary>
        /// Runs the setPropertyValue function of a control
        /// </summary>
        /// <param name="itemPath">Path to the item</param>
        /// <param name="value">New value we are setting the property to</param>
        /// <returns>True if setPropertyValue function was successfully executed.</returns>
        public Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value);

        /// <summary>
        /// Loads the object model for Power Apps
        /// </summary>
        /// <returns>Power Apps object model</returns>
        public Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync();

        /// <summary>
        /// Gets the number of items in an array
        /// </summary>
        /// <param name="itemPath">Path to the item</param>
        /// <returns>Number of items in the array</returns>
        public int GetItemCount(ItemPath itemPath);

        /// <summary>
        /// Check if app status returns 'idle' or 'busy'
        /// </summary>
        /// <returns>True if app status is idle</returns>
        public Task<bool> CheckIsIdleAsync();

        /// <summary>
        /// Get Debug Info
        /// </summary>
        public Task<object> GetDebugInfo();

        /// <summary>
        /// TestEngine ready function returns true if the functions are ready
        /// else it throws exception
        /// </summary>
        public Task<bool> TestEngineReady();
    }
}
