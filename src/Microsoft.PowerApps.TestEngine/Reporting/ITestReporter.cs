// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    /// <summary>
    /// Handles the test report generation
    /// </summary>
    public interface ITestReporter
    {
        /// <summary>
        /// Test Run App URL
        public string TestRunAppURL { get; set; }

        /// <summary>
        /// Test Results Directory
        public string TestResultsDirectory { get; set; }

        /// <summary>
        /// Creates a test run
        /// </summary>
        /// <param name="testRunName">Name of the test run</param>
        /// <param name="testRunUser">User who triggered the test run</param>
        /// <returns>Test run id</returns>
        public string CreateTestRun(string testRunName, string testRunUser);

        /// <summary>
        /// Starts a test run. This records the start time of the test run.
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        public void StartTestRun(string testRunId);

        /// <summary>
        /// Ends a test run
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        public void EndTestRun(string testRunId);

        public string CreateTestSuite(string testRunId, string testSuiteName);

        /// <summary>
        /// Creates a test in a test run
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        /// <param name="testSuiteId">Test suite id</param>
        /// <param name="testName">Name of test</param>
        /// <returns>Test id</returns>
        public string CreateTest(string testRunId, string testSuiteId, string testName);

        /// <summary>
        /// Starts test. This records the start time of the test.
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        /// <param name="testId">Test id</param>
        public void StartTest(string testRunId, string testId);

        /// <summary>
        /// End test.
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        /// <param name="testId">Test id</param>
        /// <param name="success">Whether test was successful</param>
        /// <param name="stdout">Standard output</param>
        /// <param name="additionalFiles">Any additional test files</param>
        /// <param name="errorMessage">Error message if test was unsuccessful</param>
        public void EndTest(string testRunId, string testId, bool success, string stdout, List<string> additionalFiles, string errorMessage);

        /// <summary>
        /// End test.
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        /// <param name="testId">Test id</param>
        public void FailTest(string testRunId, string testId);

        /// <summary>
        /// Generate test report
        /// </summary>
        /// <param name="testRunId">Test run id</param>
        /// <param name="resultsDirectory">Directory to place the test report</param>
        /// <returns>Path to test report</returns>
        public string GenerateTestReport(string testRunId, string resultsDirectory);
    }
}
