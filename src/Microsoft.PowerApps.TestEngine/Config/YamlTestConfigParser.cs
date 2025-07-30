// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using YamlDotNet.Core;
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

        public T ParseTestConfig<T>(string testConfigFilePath, ILogger logger)
        {
            try
            {
                if (string.IsNullOrEmpty(testConfigFilePath))
                {
                    throw new ArgumentNullException(nameof(testConfigFilePath));
                }

                if (!_fileSystem.FileExists(testConfigFilePath))
                {
                    logger.LogError($"Invalid file path: {typeof(T).Name} in test config file.");
                    throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionInvalidFilePath.ToString());
                }

                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

                return deserializer.Deserialize<T>(_fileSystem.ReadAllText(testConfigFilePath));
            }
            catch (YamlException)
            {
                logger.LogError($"Invalid YAML format: {typeof(T).Name} in test config file.");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionYAMLFormat.ToString());
            }
        }
    }
}
