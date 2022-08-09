// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using YamlDotNet.Core;
using YamlDotNet.Serialization;
namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Defines one test
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Gets or sets the name of the test case.
        /// It will be used in reporting success and failure.
        /// </summary>
        public string TestCaseName { get; set; } = "";

        /// <summary>
        /// Gets or sets the additional information that describes what the test case does.
        /// Optional.
        /// </summary>
        public string TestCaseDescription { get; set; } = "";

        /// <summary>
        /// Gets or sets the the Power FX functions describing the steps needed to perform the test.
        /// </summary>
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public string TestSteps { get; set; } = "";
    }
}
