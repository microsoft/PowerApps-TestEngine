// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerApps.TestEngine.TestStudioConverter
{
    public class ConverterTestDefinition : TestSuiteDefinition
    {
        //Hiding all the existing properties to preserve YamlDotNet writing order
        public new string TestSuiteName { get; set; } = "";

        public new string TestSuiteDescription { get; set; } = "";

        public new string Persona { get; set; } = "";

        public new string AppLogicalName { get; set; } = "";

        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public new string OnTestCaseStart { get; set; }

        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public new string OnTestCaseComplete { get; set; }

        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public new string OnTestSuiteComplete { get; set; }

        public new List<NetworkRequestMock> NetworkRequestMocks { get; set; }

        public new List<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
