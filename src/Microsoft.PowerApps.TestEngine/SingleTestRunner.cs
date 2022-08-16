﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
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

        public async Task RunTestAsync(string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig)
        {
            RunCount++;

            if (RunCount != 1)
            {
                // It is unclear what side effects show up if you run this multiple times especially relating to the logging
                // So throwing if it is run more than once.
                throw new InvalidOperationException("This test can only be run once.");
            }

            var browserConfigName = string.IsNullOrEmpty(browserConfig.ConfigName) ? browserConfig.Browser : browserConfig.ConfigName;

            var testSuiteId = _testReporter.CreateTestSuite(testRunId, $"{testSuiteDefinition.TestSuiteName} - {browserConfigName}");

            Logger = _loggerFactory.CreateLogger(testSuiteId);
            _testState.SetLogger(Logger);

            _testState.SetTestSuiteDefinition(testSuiteDefinition);
            _testState.SetTestRunId(testRunId);
            _testState.SetBrowserConfig(browserConfig);

            var testResultDirectory = Path.Combine(testRunDirectory, $"{testSuiteDefinition.TestSuiteName}_{browserConfigName}_{testSuiteId.Substring(0, 6)}");
            _testState.SetTestResultsDirectory(testResultDirectory);

            try
            {
                _fileSystem.CreateDirectory(testResultDirectory);

                Logger.LogInformation($"\n\n---------------------------------------------------------------------------\n" +
                    $"RUNNING TEST SUITE: {testSuiteDefinition.TestSuiteName}" +
                    $"\n---------------------------------------------------------------------------\n\n" +
                    $"Browser configuration: {JsonConvert.SerializeObject(browserConfig)}");


                // Set up test infra
                await _testInfraFunctions.SetupAsync();

                // Navigate to test url
                await _testInfraFunctions.GoToUrlAsync(_urlMapper.GenerateTestUrl());

                // Log in user
                await _userManager.LoginAsUserAsync();

                // Set up network request mocking if any
                await _testInfraFunctions.SetupNetworkRequestMockAsync();

                // Set up Power Fx
                _powerFxEngine.Setup();
                await _powerFxEngine.UpdatePowerFxModelAsync();

                // Run test case one by one
                foreach (var testCase in _testState.GetTestSuiteDefinition().TestCases)
                {
                    TestSuccess = true;
                    var testId = _testReporter.CreateTest(testRunId, testSuiteId, $"{testCase.TestCaseName}", "TODO");
                    _testReporter.StartTest(testRunId, testId);
                    _testState.SetTestId(testId);

                    using (var scope = Logger.BeginScope(testId))
                    {
                        var testCaseResultDirectory = Path.Combine(testResultDirectory, $"{testCase.TestCaseName}_{testId.Substring(0, 6)}");
                        _testState.SetTestResultsDirectory(testCaseResultDirectory);
                        _fileSystem.CreateDirectory(testCaseResultDirectory);

                        try
                        {
                            if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestCaseStart))
                            {
                                Logger.LogInformation($"Running OnTestCaseStart for test case: {testCase.TestCaseName}");
                                await _powerFxEngine.ExecuteWithRetryAsync(testSuiteDefinition.OnTestCaseStart);
                            }

                            Logger.LogInformation($"---------------------------------------------------------------------------\n" +
                                $"RUNNING TEST CASE: {testCase.TestCaseName}" +
                                $"\n---------------------------------------------------------------------------");

                            await _powerFxEngine.ExecuteWithRetryAsync(testCase.TestSteps);

                            if (!string.IsNullOrEmpty(testSuiteDefinition.OnTestCaseComplete))
                            {
                                Logger.LogInformation($"Running OnTestCaseComplete for test case: {testCase.TestCaseName}");
                                await _powerFxEngine.ExecuteWithRetryAsync(testSuiteDefinition.OnTestCaseComplete);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex.ToString());
                            TestException = ex;
                            TestSuccess = false;
                        }
                        finally
                        {
                            if (TestLoggerProvider.TestLoggers.ContainsKey(testSuiteId))
                            {
                                var testLogger = TestLoggerProvider.TestLoggers[testSuiteId];
                                testLogger.WriteToLogsFile(testCaseResultDirectory, testId);
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
                    Logger.LogInformation($"Running OnTestSuiteComplete for test suite: {testSuiteDefinition.TestSuiteName}");
                    _testState.SetTestResultsDirectory(testResultDirectory);
                    _powerFxEngine.Execute(testSuiteDefinition.OnTestSuiteComplete);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                TestException = ex;
            }
            finally
            {
                await _testInfraFunctions.EndTestRunAsync();

                // save log for the test suite
                if (TestLoggerProvider.TestLoggers.ContainsKey(testSuiteId))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testSuiteId];
                    testLogger.WriteToLogsFile(testResultDirectory, null);
                }
            }
        }
    }
}
