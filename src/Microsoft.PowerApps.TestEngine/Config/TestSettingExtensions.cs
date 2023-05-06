// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Test Settings for modules
    /// </summary>
    public class TestSettingExtensions
    {
        /// <summary>
        /// Determine if extension modules should be enabled
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// List of allowed Test Engine Modules that can be referenced.
        /// </summary>
        public List<string> AllowModule { get; set; } = new List<string>() { "*" };

        /// <summary>
        /// List of allowed Test Engine Modules cannot be loaded unless there is an explict allow
        /// </summary>
        public List<string> DenyModule { get; set; } = new List<string>();

        /// <summary>
        /// List of allowed .Net Namespaces that can be referenced in a Test Engine Module
        /// </summary>
        public List<string> AllowNamespaces { get; set; } = new List<string>();

        /// <summary>
        /// List of allowed .Net Namespaces that deney load unless explict allow is defined
        /// </summary>
        public List<string> DenyNamespaces { get; set; } = new List<string>();

        /// <summary>
        /// Additional optional parameters for extension modules
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}
