// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// State for a single test run
    /// </summary>
    public interface ISingleTestInstanceState
    {
        /// <summary>
        /// Set the test run id.
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        public void SetTestRunId(string testRunId);

        /// <summary>
        /// Gets the test run id.
        /// </summary>
        /// <returns>Test run id</returns>
        public string GetTestRunId();

        /// <summary>
        /// Sets the test id for this instance.
        /// </summary>
        /// <param name="testId">Test id</param>
        public void SetTestId(string testId);

        /// <summary>
        /// Gets the test id for this instance.
        /// </summary>
        /// <returns>Test id</returns>
        public string GetTestId();

        /// <summary>
        /// Sets the test suite definition for this instance.
        /// </summary>
        /// <param name="testSuiteDefinition">Test suite definition</param>
        public void SetTestSuiteDefinition(TestSuiteDefinition testSuiteDefinition);

        /// <summary>
        /// Gets the test suite definition for this instance.
        /// </summary>
        /// <returns>Test suite definition</returns>
        public TestSuiteDefinition GetTestSuiteDefinition();

        /// <summary>
        /// Sets the logger for this instance.
        /// </summary>
        /// <param name="logger">Logger</param>
        public void SetLogger(ILogger logger);

        /// <summary>
        /// Gets the logger for this instance.
        /// </summary>
        /// <returns>Logger</returns>
        public ILogger GetLogger();

        /// <summary>
        /// Sets the directory where any test artifacts and results should be stored.
        /// </summary>
        /// <param name="resultsDirectory">Path to results directory</param>
        public void SetTestResultsDirectory(string resultsDirectory);

        /// <summary>
        /// Gets the directory where any test artifacts and results should be stored.
        /// </summary>
        /// <returns>Path to results directory</returns>
        public string GetTestResultsDirectory();

        /// <summary>
        /// Sets the browser configuration the test should be run against.
        /// </summary>
        /// <param name="browserConfig">Browser config</param>
        public void SetBrowserConfig(BrowserConfiguration browserConfig);

        /// <summary>
        /// Getse the browser configuration the test should be run against.
        /// </summary>
        /// <returns>Browser config</returns>
        public BrowserConfiguration GetBrowserConfig();
    }
}
