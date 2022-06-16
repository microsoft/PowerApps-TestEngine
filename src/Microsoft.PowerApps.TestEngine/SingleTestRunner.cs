// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
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
        private readonly ILoggerProvider _loggerProvider;
        private readonly ISingleTestInstanceState _testState;
        private readonly IUrlMapper _urlMapper;
        private readonly IFileSystem _fileSystem;

        private ILogger? Logger { get; set; }
        private bool TestSuccess { get; set; } = true;
        private Exception? TestException { get; set; }
        private int RunCount { get; set; } = 0;

        public SingleTestRunner(ITestReporter testReporter, 
                                IPowerFxEngine powerFxEngine, 
                                ITestInfraFunctions testInfraFunctions, 
                                IUserManager userManager,
                                ILoggerProvider loggerProvider,
                                ISingleTestInstanceState testState,
                                IUrlMapper urlMapper,
                                IFileSystem fileSystem)
        {
            _testReporter = testReporter;
            _powerFxEngine = powerFxEngine;
            _testInfraFunctions = testInfraFunctions;
            _userManager = userManager;
            _loggerProvider = loggerProvider;
            _testState = testState;
            _urlMapper = urlMapper;
            _fileSystem = fileSystem;
        }

        public async Task RunTestAsync(string testRunId, string testRunDirectory, TestDefinition testDefinition, BrowserConfiguration browserConfig)
        {
            RunCount++;

            if (RunCount != 1)
            {
                // It is unclear what side effects show up if you run this multiple times especially relating to the logging
                // So throwing if it is run more than once.
                throw new InvalidOperationException("This test can only be run once");
            }

            var testId = _testReporter.CreateTest(testRunId, $"{testDefinition.Name}", "TODO");
            _testReporter.StartTest(testRunId, testId);
            var testResultDirectory = Path.Combine(testRunDirectory, $"{testDefinition.Name}_{testId.Substring(0, 6)}");
            Logger = _loggerProvider.CreateLogger(testId);
            _testState.SetLogger(Logger);
            _testState.SetTestDefinition(testDefinition);
            _testState.SetTestRunId(testRunId);
            _testState.SetTestId(testId);
            _testState.SetTestResultsDirectory(testResultDirectory);
            _testState.SetBrowserConfig(browserConfig);

            try
            {
                _fileSystem.CreateDirectory(testResultDirectory);
                Logger.LogInformation($"Running test: {testDefinition.Name}");
                Logger.LogInformation($"Browser configuration: {JsonConvert.SerializeObject(browserConfig)}");

                // Set up test infra
                await _testInfraFunctions.SetupAsync();

                // Log in user
                await _userManager.LoginAsUserAsync();

                // Set up network request mocking if any
                await _testInfraFunctions.SetupNetworkRequestMockAsync();

                // Navigate to app
                await _testInfraFunctions.GoToUrlAsync(_urlMapper.GenerateAppUrl());

                // Set up Power Fx
                await _powerFxEngine.SetupAsync();

                // Run test
                _powerFxEngine.Execute(_testState.GetTestDefinition().TestSteps);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                TestException = ex;
                TestSuccess = false;
            }
            finally
            {
                await _testInfraFunctions.EndTestRunAsync();
                if (TestLoggerProvider.TestLoggers.ContainsKey(testId))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testId];
                    testLogger.WriteToLogsFile(testResultDirectory);
                }
                var additionalFiles = new List<string>();
                var files = _fileSystem.GetFiles(testResultDirectory);
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        additionalFiles.Add(file);
                    }
                }
                var message = $"{{ \"TestName\": {testDefinition.Name}, \"BrowserConfiguration\": {JsonConvert.SerializeObject(browserConfig)}}}";
                _testReporter.EndTest(testRunId, testId, TestSuccess, message, additionalFiles, TestException?.Message, TestException?.StackTrace);
            }
        }
    }
}
