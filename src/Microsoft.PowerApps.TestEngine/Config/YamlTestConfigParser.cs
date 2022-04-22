// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Test config parser for YAMl test definitions
    /// </summary>
    public class YamlTestConfigParser : ITestConfigParser
    {
        public TestPlanDefinition ParseTestConfig(string testConfigFilePath)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

            return deserializer.Deserialize<TestPlanDefinition>(File.ReadAllText(testConfigFilePath));
        }
    }
}
