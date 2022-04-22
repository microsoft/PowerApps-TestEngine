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
        private TestDefinition TestDefinition { get; set; }
        private string TestRunId { get; set; }
        private string TestId { get; set; }
        private string ResultsDirectory { get; set; }
        private BrowserConfiguration BrowserConfig { get; set; }

        public void SetTestRunId(string testRunId)
        {
            TestRunId = testRunId;
        }

        public string GetTestRunId()
        {
            return TestRunId;
        }

        public void SetTestId(string testId)
        {
            TestId = testId;
        }

        public string GetTestId()
        {
            return TestId;
        }

        public void SetTestDefinition(TestDefinition testDefinition)
        {
            TestDefinition = testDefinition;
        }

        public TestDefinition GetTestDefinition()
        {
            return TestDefinition;
        }

        public void SetLogger(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger GetLogger()
        {
            return Logger;
        }

        public void SetTestResultsDirectory(string resultsDirectory)
        {
            ResultsDirectory = resultsDirectory;
        }

        public string GetTestResultsDirectory()
        {
            return ResultsDirectory;
        }

        public void SetBrowserConfig(BrowserConfiguration browserConfig)
        {
            BrowserConfig = browserConfig;
        }

        public BrowserConfiguration GetBrowserConfig()
        {
            return BrowserConfig;
        }
    }
}
