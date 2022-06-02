// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    /// <summary>
    /// Object model for a control returned from javascript
    /// </summary>
    public class JSControlModel
    {        
        /// <summary>
        /// Gets or sets the name of the control
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the properties of the control
        /// </summary>
        public string[]? Properties { get; set; }

        /// <summary>
        /// Gets or sets children controls
        /// </summary>
        public JSControlModel[]? ChildrenControls { get; set; }

        /// <summary>
        /// Gets or sets the number of items if it is an array
        /// </summary>
        public int? ItemCount { get; set; }

        /// <summary>
        /// Gets or sets whether this control is of an array type
        /// </summary>
        public bool IsArray { get; set; }
    }
}
