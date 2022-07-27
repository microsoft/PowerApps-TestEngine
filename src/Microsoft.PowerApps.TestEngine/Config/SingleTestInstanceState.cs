// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// State for a single test run
    /// </summary>
    public class SingleTestInstanceState : ISingleTestInstanceState
    {
        private ILogger Logger { get; set; }
        private TestSuiteDefinition TestSuiteDefinition { get; set; }
        private string TestRunId { get; set; }
        private string TestId { get; set; }
        private string ResultsDirectory { get; set; }
        private BrowserConfiguration BrowserConfig { get; set; }

        public void SetTestRunId(string testRunId)
        {
            if (string.IsNullOrEmpty(testRunId))
            {
                GetLogger().LogCritical("Test Run ID '" + nameof(testRunId) + "' cannot be set to a null value.");
                throw new ArgumentNullException(nameof(testRunId));
            }
            TestRunId = testRunId;
        }

        public string GetTestRunId()
        {
            return TestRunId;
        }

        public void SetTestId(string testId)
        {
            if (string.IsNullOrEmpty(testId))
            {
                GetLogger().LogCritical("Test ID '" + nameof(testId) + "' cannot be set to a null value.");
                throw new ArgumentNullException(nameof(testId));
            }
            TestId = testId;
        }

        public string GetTestId()
        {
            return TestId;
        }

        public void SetTestSuiteDefinition(TestSuiteDefinition testSuiteDefinition)
        {
            if (testSuiteDefinition == null)
            {
                GetLogger().LogCritical("Test Suite Definition '" + nameof(testSuiteDefinition) + "' cannot be set to a null value.");
                throw new ArgumentNullException(nameof(testSuiteDefinition));
            }
            TestSuiteDefinition = testSuiteDefinition;
        }

        public TestSuiteDefinition GetTestSuiteDefinition()
        {
            return TestSuiteDefinition;
        }

        public void SetLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("Logger '" + nameof(logger) + "cannot be set to a null value.");
            }
            Logger = logger;
        }

        public ILogger GetLogger()
        {
            return Logger;
        }

        public void SetTestResultsDirectory(string resultsDirectory)
        {
            if (string.IsNullOrEmpty(resultsDirectory))
            {
                GetLogger().LogCritical("Results Directory '" + nameof(resultsDirectory) + "' cannot set to a null value.");
                throw new ArgumentNullException(nameof(resultsDirectory));
            }
            ResultsDirectory = resultsDirectory;
        }

        public string GetTestResultsDirectory()
        {
            return ResultsDirectory;
        }

        public void SetBrowserConfig(BrowserConfiguration browserConfig)
        {
            if (browserConfig == null)
            {
                GetLogger().LogCritical("Results Directory '" + nameof(browserConfig) + "' cannot be set to a null value.");
                throw new ArgumentNullException(nameof(browserConfig));
            }
            BrowserConfig = browserConfig;
        }

        public BrowserConfiguration GetBrowserConfig()
        {
            return BrowserConfig;
        }
    }
}
