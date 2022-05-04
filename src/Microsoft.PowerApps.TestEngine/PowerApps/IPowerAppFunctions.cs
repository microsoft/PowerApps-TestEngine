// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    public interface IPowerAppFunctions
    {
        /// <summary>
        /// Gets the value of a property from a control
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="controlName">Name of control</param>
        /// <param name="propertyName">Name of property</param>
        /// <param name="parentControlName">Name of parent control if any</param>
        /// <param name="rowOrColumnNumber">Row or column number if any</param>
        /// <returns>Property value</returns>
        public Task<T> GetPropertyValueFromControlAsync<T>(string controlName, string propertyName, string? parentControlName, int? rowOrColumnNumber);

        /// <summary>
        /// Runs the onSelect function of a control
        /// </summary>
        /// <param name="controlName"></param>
        /// <param name="controlName">Name of control</param>
        /// <param name="parentControlName">Name of parent control if any</param>
        /// <param name="rowOrColumnNumber">Row or column number if any</param>>
        /// <returns>True if onSelect function was successfully executed.</returns>
        public Task<bool> SelectControlAsync(string controlName, string? parentControlName, int? rowOrColumnNumber);

        /// <summary>
        /// Loads the object model for Power Apps
        /// </summary>
        /// <returns>Power Apps object model</returns>
        public Task<List<PowerAppControlModel>> LoadPowerAppsObjectModelAsync();
    }
}
