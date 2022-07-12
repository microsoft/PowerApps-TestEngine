﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Runs one single test
    /// </summary>
    public interface ISingleTestRunner
    {
        /// <summary>
        /// Run single test
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        /// <param name="testRunDirectory">Directory for test run</param>
        /// <param name="testSuiteDefinition">Definition of test suite</param>
        /// <param name="browserConfig">Brower to run test on</param>
        /// <returns>Task</returns>
        public Task RunTestAsync(string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig);
    }
}
