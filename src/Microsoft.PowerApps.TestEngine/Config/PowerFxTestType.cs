// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines the user defined Power Fx test type to be registered in the test engine.
    /// </summary>
    public class PowerFxTestType
    {
        /// <summary>
        /// The name of the user defined type
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The Power Fx that will evaluate to a Type
        /// </summary>
        public string Value { get; set; } = "";
    }
}
