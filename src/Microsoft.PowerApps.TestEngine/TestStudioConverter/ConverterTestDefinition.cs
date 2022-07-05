// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerApps.TestEngine.TestStudioConverter
{
    public class ConverterTestDefinition: TestDefinition
    {
        //Hiding all the existing properties to preserve YamlDotNet writing order
        public new string Name { get; set; } = "";

        public new string Description { get; set; } = "";

        public new string Persona { get; set; } = "";

        public new string AppLogicalName { get; set; } = "";

        public new List<NetworkRequestMock>? NetworkRequestMocks { get; set; }
        [YamlMember(ScalarStyle = ScalarStyle.Literal)]
        public new string TestSteps { get; set; } = "";
    }
}
