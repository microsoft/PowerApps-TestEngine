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
        public bool Enable { get; set; } = true;

        public TestSettingExtensionSource Source { get; set; } = new TestSettingExtensionSource() { };

        /// <summary>
        /// Determine if extension modules should be checks for Namespace and Certificate rules
        /// </summary>
#if RELEASE
        public bool CheckAssemblies { get; } = true;
#else
        public bool CheckAssemblies { get; set; } = true;
#endif

        /// <summary>
        /// List of allowed Test Engine Modules that can be referenced.
        /// </summary>
        public HashSet<string> AllowModule { get; set; } = new HashSet<string>() { "*" };

        /// <summary>
        /// List of allowed Test Engine Modules cannot be loaded unless there is an explict allow
        /// </summary>
        public HashSet<string> DenyModule { get; set; } = new HashSet<string>();

        /// <summary>
        /// List of allowed .Net Namespaces that can be referenced in a Test Engine Module
        /// </summary>
#if RELEASE
        //restricting for current milestone 1
        public HashSet<string> AllowNamespaces { get; } = new HashSet<string>();
#else
        public HashSet<string> AllowNamespaces { get; set; } = new HashSet<string>();
#endif

        /// <summary>
        /// List of allowed .Net Namespaces that deney load unless explict allow is defined
        /// </summary>
        public HashSet<string> DenyNamespaces { get; set; } = new HashSet<string>();

        /// <summary>
        /// List of allowed PowerFx Namespaces that can be referenced in a Test Engine Module
        /// </summary>
        public HashSet<string> AllowPowerFxNamespaces { get; set; } = new HashSet<string>();

        /// <summary>
        /// List of allowed PowerFx Namespaces that deny load unless explict allow is defined
        /// </summary>
        public HashSet<string> DenyPowerFxNamespaces { get; set; } = new HashSet<string>();


        /// <summary>
        /// Additional optional parameters for extension modules
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Optional list of scans that can be run on the workspace
        /// </summary>
        public Dictionary<string, string> Scans { get; set; } = new Dictionary<string, string>();
    }
}
