// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
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
        private readonly IPowerAppFunctions _powerAppFunctions;
        private readonly ILoggerProvider _loggerProvider;
        private readonly ISingleTestInstanceState _testState;
        private readonly IUrlMapper _urlMapper;

        private ILogger? Logger { get; set; }

        public SingleTestRunner(ITestReporter testReporter, 
                                IPowerFxEngine powerFxEngine, 
                                ITestInfraFunctions testInfraFunctions, 
                                IUserManager userManager,
                                IPowerAppFunctions powerAppFunctions,
                                ILoggerProvider loggerProvider,
                                ISingleTestInstanceState testState,
                                IUrlMapper urlMapper)
        {
            _testReporter = testReporter;
            _powerFxEngine = powerFxEngine;
            _testInfraFunctions = testInfraFunctions;
            _userManager = userManager;
            _powerAppFunctions = powerAppFunctions;
            _loggerProvider = loggerProvider;
            _testState = testState;
            _urlMapper = urlMapper;
        }

        public async Task RunTestAsync(string testRunId, string testRunDirectory, TestDefinition testDefinition, BrowserConfiguration browserConfig)
        {
            var testId = _testReporter.CreateTest(testRunId, $"{testDefinition.Name}", "TODO");
            _testReporter.StartTest(testRunId, testId);
            var testSuccess = true;
            var testResultDirectory = $"{testRunDirectory}/{testDefinition.Name}_{testId.Substring(0, 6)}";
            Logger = _loggerProvider.CreateLogger(testId);
            _testState.SetLogger(Logger);
            _testState.SetTestDefinition(testDefinition);
            _testState.SetTestRunId(testRunId);
            _testState.SetTestId(testId);
            _testState.SetTestResultsDirectory(testResultDirectory);
            _testState.SetBrowserConfig(browserConfig);

            String errorMessage = null;
            String stackTrace = null;

            try
            {
                Directory.CreateDirectory(testResultDirectory);
                Logger.LogInformation($"Running test: {testDefinition.Name}");
                Logger.LogInformation($"Browser configuration: {JsonConvert.SerializeObject(browserConfig)}");

                // Set up Power Fx
                _powerFxEngine.Setup();

                // Set up test infra
                await _testInfraFunctions.SetupAsync();

                // Log in user
                await _userManager.LoginAsUserAsync();

                // Navigate to app
                await _testInfraFunctions.GoToUrlAsync(_urlMapper.GenerateAppUrl());

                // Wait for app to load and load the object model into memory
                var controlObjectModel = await _powerAppFunctions.LoadPowerAppsObjectModelAsync();
                foreach (var control in controlObjectModel)
                {
                    _powerFxEngine.UpdateVariable(control.Name, control);
                }

                // Run test

                // Temporary hack until Power FX enables the ; parsing flag
                var testStepsArray = testDefinition.TestSteps.Split(';');

                foreach (var testStep in testStepsArray)
                {
                    var powerFxSuccess = _powerFxEngine.Execute(testStep);
                    if (!powerFxSuccess)
                    {
                        testSuccess = false;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                errorMessage = ex.Message;
                stackTrace = ex.StackTrace;
                testSuccess = false;
            }
            finally
            {
                Logger.LogInformation("Ending test");
                await _testInfraFunctions.EndTestRunAsync();
                if (TestLoggerProvider.TestLoggers.ContainsKey(testId))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testId];
                    testLogger.WriteToLogsToFile(testResultDirectory);
                }
                var additionalFiles = new List<string>();
                var files = Directory.GetFiles(testResultDirectory);
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        additionalFiles.Add(file);
                    }
                }
                var message = $"{{ \"TestName\": {testDefinition.Name}, \"BrowserConfiguration\": {JsonConvert.SerializeObject(browserConfig)}}}";
                _testReporter.EndTest(testRunId, testId, testSuccess, message, additionalFiles, errorMessage, stackTrace);
            }
        }
    }
}
