// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines one test
    /// </summary>
    public class TestSuiteDefinition
    {
        /// <summary>
        /// Gets or sets the name of the test suite.
        /// </summary>
        public string TestSuiteName { get; set; } = "";

        /// <summary>
        /// Gets or sets the additional information that describes what the test suite does.
        /// Optional.
        /// </summary>
        public string TestSuiteDescription { get; set; } = "";

        /// <summary>
        /// Gets or sets the user that will be logged in to perform the test.
        /// This must match a personal listed in the user configuration.
        /// </summary>
        public string Persona { get; set; } = "";

        /// <summary>
        /// Gets or sets the logical name of the app to be launched.
        /// It can be obtained from the solution.
        /// For canvas apps, you need to add it to a solution to obtain it.
        /// </summary>
        public string AppLogicalName { get; set; } = "";

        /// <summary>
        /// Gets or sets the id of the app to be launched
        /// This will be used only when app logical name is not present
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Power FX functions that need to be triggered
        /// for every test case in a suite before the case begins executing.
        /// </summary>
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string OnTestCaseStart { get; set; }

        /// <summary>
        /// Gets or sets the Power FX functions that need to be triggered
        /// for every test case in a suite after the case finishes executing.
        /// </summary>
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string OnTestCaseComplete { get; set; }

        /// <summary>
        /// Gets or sets the Power FX functions that need to be triggered after the suite finishes executing.
        /// </summary>
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string OnTestSuiteComplete { get; set; }

        /// <summary>
        /// Gets or sets the network requests to be mocked.
        /// </summary>
        public List<NetworkRequestMock> NetworkRequestMocks { get; set; }

        /// <summary>
        /// Gets or sets the test cases to be executed.
        /// </summary>
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
