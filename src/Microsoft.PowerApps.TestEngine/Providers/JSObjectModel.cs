// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Object model returned from javascript
    /// </summary>
    public class JSObjectModel
    {
        /// <summary>
        /// List of controls object model
        /// </summary>
        public List<JSControlModel> Controls { get; set; }
    }
}
