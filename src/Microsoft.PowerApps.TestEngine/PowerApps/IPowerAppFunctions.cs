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
        /// <param name="itemPath">Path to the item</param>
        /// <returns>Property value</returns>
        public Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath);

        /// <summary>
        /// Runs the onSelect function of a control
        /// </summary>
        /// <param name="itemPath">Path to the item</param>
        /// <returns>True if onSelect function was successfully executed.</returns>
        public Task<bool> SelectControlAsync(ItemPath itemPath);

        /// <summary>
        /// Loads the object model for Power Apps
        /// </summary>
        /// <returns>Power Apps object model</returns>
        public Task<List<PowerAppControlModel>> LoadPowerAppsObjectModelAsync();
    }
}
