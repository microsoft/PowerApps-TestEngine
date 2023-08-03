// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerApps.PowerFxModel;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    public interface IPowerAppFunctions
    {
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
        public Task<Dictionary<string, ControlRecordValue>> LoadPowerAppsObjectModelAsync();

        /// <summary>
        /// Gets the number of items in an array
        /// </summary>
        /// <param name="itemPath">Path to the item</param>
        /// <returns>Number of items in the array</returns>
        public int GetItemCount(ItemPath itemPath);

        /// <summary>
        /// Verify if using legacy player
        /// </summary>
        public Task CheckAndHandleIfLegacyPlayerAsync();

        /// <summary>
        /// Check if app status returns 'idle' or 'busy'
        /// </summary>
        /// <returns>True if app status is idle</returns>
        public Task<bool> CheckIfAppIsIdleAsync();

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
