// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Runs one single test
    /// </summary>
    public class SingleTestRunner : ISingleTestRunner
    {
        private readonly ITestReporter _testReporter;
        private readonly IPowerFxEngine _powerFxEngine;
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly IUserManager _userManager;
        private readonly ISingleTestInstanceState _testState;
        private readonly IUrlMapper _urlMapper;
        private readonly IFileSystem _fileSystem;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITestEngineEvents _eventHandler;
        private ILogger Logger { get; set; }

        private bool TestSuccess { get; set; } = true;
        private Exception TestException { get; set; }
        private int RunCount { get; set; } = 0;

        public SingleTestRunner(ITestReporter testReporter,
                                IPowerFxEngine powerFxEngine,
                                ITestInfraFunctions testInfraFunctions,
                                IUserManager userManager,
                                ISingleTestInstanceState testState,
                                IUrlMapper urlMapper,
                                IFileSystem fileSystem,
                                ILoggerFactory loggerFactory,
                                ITestEngineEvents eventHandler)
        {
            _testReporter = testReporter;
            _powerFxEngine = powerFxEngine;
            _testInfraFunctions = testInfraFunctions;
            _userManager = userManager;
            _testState = testState;
            _urlMapper = urlMapper;
            _fileSystem = fileSystem;
            _loggerFactory = loggerFactory;
            _eventHandler = eventHandler;
        }

        public async Task RunTestAsync(string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig, string domain, string queryParams, CultureInfo locale)
        {
            RunCount++;

            if (RunCount != 1)
            {
                // It is unclear what side effects show up if you run this multiple times especially relating to the logging
                // So throwing if it is run more than once.
                throw new InvalidOperationException("This test can only be run once.");
            }

            // This flag is needed to check if test run is skipped
            var allTestsSkipped = true;

            var casesTotal = 0;
            var casesPass = 0;

            var browserConfigName = string.IsNullOrEmpty(browserConfig.ConfigName) ? browserConfig.Browser : browserConfig.ConfigName;
            var testSuiteName = testSuiteDefinition.TestSuiteName;
            var testSuiteId = _testReporter.CreateTestSuite(testRunId, $"{testSuiteName} - {browserConfigName}");
            var desiredUrl = "";

            Logger = _loggerFactory.CreateLogger(testSuiteId);
            _testState.SetLogger(Logger);

            _testState.SetTestSuiteDefinition(testSuiteDefinition);
            _testState.SetTestRunId(testRunId);
            _testState.SetBrowserConfig(browserConfig);

            var testResultDirectory = Path.Combine(testRunDirectory, $"{_fileSystem.RemoveInvalidFileNameChars(testSuiteName)}_{browserConfigName}_{testSuiteId.Substring(0, 6)}");
            _testState.SetTestResultsDirectory(testResultDirectory);

            casesTotal = _testState.GetTestSuiteDefinition().TestCases.Count();

            // Number of total cases are recorded and also initialize the passed cases to 0 for this test run
            _eventHandler.SetAndInitializeCounters(casesTotal);

            string suiteException = null;

            try
            {
                _testReporter.TestResultsDirectory = testRunDirectory;
                _fileSystem.CreateDirectory(testResultDirectory);

                Logger.LogInformation($"\n\n---------------------------------------------------------------------------\n" +
                    $"RUNNING TEST SUITE: {testSuiteName}" +
                    $"\n---------------------------------------------------------------------------\n\n");
                Logger.LogInformation($"Browser configuration: {JsonConvert.SerializeObject(browserConfig)}");

                // Set up test infra
                await _testInfraFunctions.SetupAsync();
                Logger.LogInformation("Test infrastructure setup finished");

                desiredUrl = _urlMapper.GenerateTestUrl(domain, queryParams);
                Logger.LogInformation($"Desired URL: {desiredUrl}");

                _eventHandler.SuiteBegin(testSuiteName, testRunDirectory, browserConfigName, desiredUrl);

                // Navigate to test url
                await _testInfraFunctions.GoToUrlAsync(desiredUrl);
                Logger.LogInformation("Successfully navigated to target URL");

                _testReporter.TestRunAppURL = desiredUrl;

                // Log in user
                await _userManager.LoginAsUserAsync(desiredUrl);

                // Set up Power Fx
                _powerFxEngine.Setup();
                await _powerFxEngine.RunRequirementsCheckAsync();
                await _powerFxEngine.UpdatePowerFxModelAsync();

                // Set up network request mocking if any
                await _testInfraFunctions.SetupNetworkRequestMockAsync();

                allTestsSkipped = false;

                // Run test case one by one
                foreach (var testCase in _testState.GetTestSuiteDefinition().TestCases)
                {
                    _eventHandler.TestCaseBegin(testCase.TestCaseName);

                    TestSuccess = true;
                    var testId = _testReporter.CreateTest(testRunId, testSuiteId, $"{testCase.TestCaseName}");
                    _testReporter.StartTest(testRunId, testId);
                    _testState.SetTestId(testId);

                    using (var scope = Logger.BeginScope(testId))
                    {

                        var testCaseResultDirectory = Path.Combine(testResultDirectory, $"{testCase.TestCaseName}_{testId.Substring(0, 6)}");
                        _testState.SetTestResultsDirectory(testCaseResultDirectory);
                        _fileSystem.CreateDirectory(testCaseResultDirectory);
                        string caseException = null;

                        try
                        {
                            Logger.LogInformation($"---------------------------------------------------------------------------\n" +
                                $"RUNNING TEST CASE: {testCase.TestCaseName}" +
                                $"\n---------------------------------------------------------------------------");

                            if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestCaseStart))
                            {
                                Logger.LogInformation($"Running OnTestCaseStart for test case: {testCase.TestCaseName}");
                                await _powerFxEngine.ExecuteWithRetryAsync(testSuiteDefinition.OnTestCaseStart, locale);
                            }

                            await _powerFxEngine.ExecuteWithRetryAsync(testCase.TestSteps, locale);

                            if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestCaseComplete))
                            {
                                Logger.LogInformation($"Running OnTestCaseComplete for test case: {testCase.TestCaseName}");
                                await _powerFxEngine.ExecuteWithRetryAsync(testSuiteDefinition.OnTestCaseComplete, locale);
                            }

                            _eventHandler.TestCaseEnd(true);
                            casesPass++;
                        }
                        catch (Exception ex)
                        {
                            _eventHandler.EncounteredException(ex);
                            _eventHandler.TestCaseEnd(false);

                            caseException = ex.ToString();
                            TestException = ex;
                            TestSuccess = false;

                            Logger.LogError("Encountered an error. See the debug log for this test case for more information.");
                        }
                        finally
                        {
                            if (TestLoggerProvider.TestLoggers.ContainsKey(testSuiteId))
                            {
                                var testLogger = TestLoggerProvider.TestLoggers[testSuiteId];
                                testLogger.WriteToLogsFile(testCaseResultDirectory, testId);
                            }

                            if (!string.IsNullOrEmpty(caseException))
                            {
                                var testLogger = TestLoggerProvider.TestLoggers[testSuiteId];
                                testLogger.WriteExceptionToDebugLogsFile(testCaseResultDirectory, caseException);
                            }

                            var additionalFiles = new List<string>();
                            var files = _fileSystem.GetFiles(testCaseResultDirectory);
                            if (files != null)
                            {
                                foreach (var file in files)
                                {
                                    additionalFiles.Add(file);
                                }
                            }

                            var message = $"{{ \"BrowserConfiguration\": {JsonConvert.SerializeObject(browserConfig)}}}";
                            _testReporter.EndTest(testRunId, testId, TestSuccess, message, additionalFiles, TestException?.Message);
                        }
                    }
                }

                // Execute OnTestSuiteComplete
                if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestSuiteComplete))
                {
                    Logger.LogInformation($"Running OnTestSuiteComplete for test suite: {testSuiteName}");
                    _testState.SetTestResultsDirectory(testResultDirectory);
                    _powerFxEngine.Execute(testSuiteDefinition.OnTestSuiteComplete, locale);
                }
            }
            catch (UserInputException ex)
            {
                _eventHandler.EncounteredException(ex);
            }
            catch (Exception ex)
            {
                Logger.LogError("Encountered an error. See the debug log for this test suite for more information.");
                suiteException = ex.ToString();
                TestException = ex;
            }
            finally
            {
                // Trying to log the debug info including session details
                // Consider avoiding calling DebugInfo in cases where the PowerAppsTestEngine object is not needed
                // Like exceptions thrown during initialization failures or user input errors
                LoggingHelper loggingHelper = new LoggingHelper(_powerFxEngine.GetPowerAppFunctions(), _testState, _eventHandler);
                loggingHelper.DebugInfo();

                await _testInfraFunctions.EndTestRunAsync();

                if (allTestsSkipped)
                {
                    // Run test case one by one, mark it as failed
                    foreach (var testCase in _testState.GetTestSuiteDefinition().TestCases)
                    {
                        var testId = _testReporter.CreateTest(testRunId, testSuiteId, $"{testCase.TestCaseName}");
                        _testReporter.FailTest(testRunId, testId);
                    }
                }

                string summaryString = $"\nTest suite summary\nTotal cases: {casesTotal}" +
                                $"\nCases passed: {casesPass}" +
                                $"\nCases failed: {(casesTotal - casesPass)}";

                Logger.LogInformation(summaryString);
                _eventHandler.SuiteEnd();

                // save log for the test suite
                if (TestLoggerProvider.TestLoggers.ContainsKey(testSuiteId))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testSuiteId];
                    testLogger.WriteToLogsFile(testResultDirectory, null);
                }

                if (!string.IsNullOrEmpty(suiteException))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testSuiteId];
                    testLogger.WriteExceptionToDebugLogsFile(testResultDirectory, suiteException);
                }
                await _testInfraFunctions.DisposeAsync();
            }
        }
    }
}
