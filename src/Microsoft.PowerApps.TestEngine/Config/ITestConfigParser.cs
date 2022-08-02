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
        public T ParseTestConfig<T>(string testConfigFilePath, ILogger logger);
    }
}
