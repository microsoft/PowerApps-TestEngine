// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Describes the path to access an item
    /// </summary>
    public class ItemPath
    {
        /// <summary>
        /// Gets or sets the control name
        /// </summary>
        [JsonProperty("controlName")]
        public string ControlName { get; set; }
        /// <summary>
        /// Gets or sets the index of the control. Optional. Used in Gallery
        /// </summary>
        [JsonProperty("index")]
        public int? Index { get; set; }
        /// <summary>
        /// Gets or sets the parent control to access. Optional. Used in any nested control scenarios
        /// </summary>
        [JsonProperty("parentControl")]
        public ItemPath ParentControl { get; set; }
        /// <summary>
        /// Gets or sets the property name of a control. Optional.
        /// </summary>
        [JsonProperty("propertyName")]
        public string PropertyName { get; set; }
    }
}
