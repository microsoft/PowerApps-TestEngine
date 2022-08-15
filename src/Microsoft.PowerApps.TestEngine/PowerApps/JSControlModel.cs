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
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the properties of the control
        /// </summary>
        public JSPropertyModel[] Properties { get; set; }
    }
}
