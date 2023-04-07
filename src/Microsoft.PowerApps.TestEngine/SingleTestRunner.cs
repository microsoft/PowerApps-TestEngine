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
                                ILoggerFactory loggerFactory)
        {
            _testReporter = testReporter;
            _powerFxEngine = powerFxEngine;
            _testInfraFunctions = testInfraFunctions;
            _userManager = userManager;
            _testState = testState;
            _urlMapper = urlMapper;
            _fileSystem = fileSystem;
            _loggerFactory = loggerFactory;
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

            string suiteException = null;

            try
            {
                _fileSystem.CreateDirectory(testResultDirectory);

                Logger.LogInformation($"\n\n---------------------------------------------------------------------------\n" +
                    $"RUNNING TEST SUITE: {testSuiteName}" +
                    $"\n---------------------------------------------------------------------------\n\n");
                Logger.LogTrace( $"Browser configuration: {JsonConvert.SerializeObject(browserConfig)}");
                
                // Set up test infra
                await _testInfraFunctions.SetupAsync();
                Logger.LogDebug("Test infrastructure setup finished");

                desiredUrl = _urlMapper.GenerateTestUrl(domain, queryParams);
                Logger.LogInformation($"Desired URL: {desiredUrl}");

                var startString = $"\nRunning test suite: {testSuiteName}" +
                $"\n\tTest results will be stored in: {testRunDirectory}" +
                $"\n\tBrowser: {browserConfigName}" +
                $"\n\tApp URL: {desiredUrl.Replace("&source=testengine", String.Empty)}";

                Console.Out.WriteLine(startString);

                // Navigate to test url
                await _testInfraFunctions.GoToUrlAsync(desiredUrl);
                Logger.LogDebug("Successfully navigated to target URL");

                // Log in user
                await _userManager.LoginAsUserAsync(desiredUrl);

                // Set up network request mocking if any
                await _testInfraFunctions.SetupNetworkRequestMockAsync();

                // Set up Power Fx
                _powerFxEngine.Setup(locale);
                await _powerFxEngine.UpdatePowerFxModelAsync();

                allTestsSkipped = false;

                // Run test case one by one
                foreach (var testCase in _testState.GetTestSuiteDefinition().TestCases)
                {
                    Console.Out.WriteLine($"\nTest case: {testCase.TestCaseName}");

                    TestSuccess = true;
                    var testId = _testReporter.CreateTest(testRunId, testSuiteId, $"{testCase.TestCaseName}", "TODO");
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
                                await _powerFxEngine.ExecuteWithRetryAsync(testSuiteDefinition.OnTestCaseStart);
                            }

                            await _powerFxEngine.ExecuteWithRetryAsync(testCase.TestSteps);

                            if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestCaseComplete))
                            {
                                Logger.LogInformation($"Running OnTestCaseComplete for test case: {testCase.TestCaseName}");
                                await _powerFxEngine.ExecuteWithRetryAsync(testSuiteDefinition.OnTestCaseComplete);
                            }

                            Console.Out.WriteLine("  Result: Passed");
                            casesPass++;
                        }
                        catch (Exception ex)
                        {
                            Console.Out.WriteLine($"  Assertion failed: {ex.Message}");
                            Console.Out.WriteLine("  Result: Failed");

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

                            var message = $"{{ \"TestName\": {testCase.TestCaseName}, \"BrowserConfiguration\": {JsonConvert.SerializeObject(browserConfig)}}}";
                            _testReporter.EndTest(testRunId, testId, TestSuccess, message, additionalFiles, TestException?.Message, TestException?.StackTrace);
                        }
                    }
                }

                // Execute OnTestSuiteComplete
                if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestSuiteComplete))
                {
                    Logger.LogInformation($"Running OnTestSuiteComplete for test suite: {testSuiteName}");
                    _testState.SetTestResultsDirectory(testResultDirectory);
                    _powerFxEngine.Execute(testSuiteDefinition.OnTestSuiteComplete);
                }
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
                LoggingHelper loggingHelper = new LoggingHelper(_powerFxEngine.GetPowerAppFunctions(), _testState);
                loggingHelper.DebugInfo();

                await _testInfraFunctions.EndTestRunAsync();

                if (allTestsSkipped)
                {
                    // Run test case one by one, mark it as failed
                    foreach (var testCase in _testState.GetTestSuiteDefinition().TestCases)
                    {
                        var testId = _testReporter.CreateTest(testRunId, testSuiteId, $"{testCase.TestCaseName}", "TODO");
                        _testReporter.FailTest(testRunId, testId);
                    }
                }

                string summaryString = $"\nTest suite summary\nTotal cases: {casesTotal}" +
                                $"\nCases passed: {casesPass}" +
                                $"\nCases failed: {(casesTotal - casesPass)}";

                Logger.LogInformation(summaryString);
                Console.Out.WriteLine(summaryString);

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
            }
        }
    }
}
