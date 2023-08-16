// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// Handles parsing the test configuration
    /// </summary>
    public interface ITestConfigParser
    {
        /// <summary>
        /// Parses test config paths.
        /// </summary>
        /// <param name="testConfigFilePath">Config file path for test</param>
        /// <param name="logger">Logger</param>
        public T ParseTestConfig<T>(string testConfigFilePath, ILogger logger);
    }
}
