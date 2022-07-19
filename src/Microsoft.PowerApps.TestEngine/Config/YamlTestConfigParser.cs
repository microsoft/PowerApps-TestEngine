// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.System;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Test config parser for YAMl test definitions
    /// </summary>
    public class YamlTestConfigParser : ITestConfigParser
    {
        private readonly IFileSystem _fileSystem;

        public YamlTestConfigParser(IFileSystem fileSytem)
        {
            _fileSystem = fileSytem;
        }

        public T ParseTestConfig<T>(string testConfigFilePath)
        {
            if (string.IsNullOrEmpty(testConfigFilePath))
            {
                throw new ArgumentNullException(nameof(testConfigFilePath));
            }

            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

            return deserializer.Deserialize<T>(_fileSystem.ReadAllText(testConfigFilePath));
        }
    }
}
