// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Test engine
    /// </summary>
    public class TestEngine
    {
        private readonly ITestState _state;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestReporter _testReporter;
        private readonly IFileSystem _fileSystem;
        private readonly ILoggerFactory _loggerFactory;
        private const string DefaultOutputDirectory = "TestOutput";
        private const string DefaultCloud = "Prod";
        private ILogger Logger { get; set; }

        public TestEngine(ITestState state,
                          IServiceProvider serviceProvider,
                          ITestReporter testReporter,
                          IFileSystem fileSystem,
                          ILoggerFactory loggerFactory)
        {
            _state = state;
            _serviceProvider = serviceProvider;
            _testReporter = testReporter;
            _fileSystem = fileSystem;
            _loggerFactory = loggerFactory;
        }

        public async Task<string> RunTestAsync(string testConfigFile, string environmentId, string tenantId, string outputDirectory, string cloud)
        {
            // Set up test reporting
            var testRunId = _testReporter.CreateTestRun("Power Fx Test Runner", "User"); // TODO: determine if there are more meaningful values we can put here
            _testReporter.StartTestRun(testRunId);

            Logger = _loggerFactory.CreateLogger(testRunId);

            if (string.IsNullOrEmpty(outputDirectory))
            {
                Logger.LogDebug($"Using default output directory: {DefaultOutputDirectory}");
                _state.SetOutputDirectory(DefaultOutputDirectory);
            }
            else
            {
                Logger.LogDebug($"Using output directory: {outputDirectory}");
                _state.SetOutputDirectory(outputDirectory);
            }

            var testRunDirectory = Path.Combine(_state.GetOutputDirectory(), testRunId.Substring(0, 6));
            _fileSystem.CreateDirectory(testRunDirectory);
            Logger.LogInformation($"Test results will be stored in: {testRunDirectory}");

            try
            {

                // Setup state
                if (string.IsNullOrEmpty(testConfigFile))
                {
                    Logger.LogError("Test config file cannot be null");
                    throw new ArgumentNullException(nameof(testConfigFile));
                }

                if (string.IsNullOrEmpty(environmentId))
                {
                    Logger.LogError("Environment id cannot be null");
                    throw new ArgumentNullException(nameof(environmentId));
                }

                if (string.IsNullOrEmpty(tenantId))
                {
                    Logger.LogError("Tenant id cannot be null");
                    throw new ArgumentNullException(nameof(tenantId));
                }

                _state.ParseAndSetTestState(testConfigFile);
                _state.SetEnvironment(environmentId);
                _state.SetTenant(tenantId);

                if (string.IsNullOrEmpty(cloud))
                {
                    Logger.LogDebug($"Using default cloud: {DefaultCloud}");
                    _state.SetCloud(DefaultCloud);
                }
                else
                {
                    Logger.LogDebug($"Using cloud: {cloud}");
                    _state.SetCloud(cloud);
                }

                await RunTestByWorkerCountAsync(testRunId, testRunDirectory);
                _testReporter.EndTestRun(testRunId);
                return _testReporter.GenerateTestReport(testRunId, testRunDirectory);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                throw;
            }
            finally
            {
                if (TestLoggerProvider.TestLoggers.ContainsKey(testRunId))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testRunId];
                    testLogger.WriteToLogsFile(testRunDirectory, null);
                }
            }
        }

        public async Task RunTestByWorkerCountAsync(string testRunId, string testRunDirectory)
        {
            var browserConfigurations = _state.GetTestSettings().BrowserConfigurations;
            var allTestRuns = new List<Task>();

            // Manage number of workers
            foreach (var browserConfig in browserConfigurations)
            {
                allTestRuns.Add(Task.Run(() => RunOneTestAsync(testRunId, testRunDirectory, _state.GetTestSuiteDefinition(), browserConfig)));
                if (allTestRuns.Count >= _state.GetWorkerCount())
                {
                    Logger.LogDebug($"Waiting for {allTestRuns.Count} test runs to complete");
                    await Task.WhenAll(allTestRuns.ToArray());
                    allTestRuns.Clear();
                }
            }

            Logger.LogDebug($"Waiting for {allTestRuns.Count} test runs to complete");
            await Task.WhenAll(allTestRuns.ToArray());
        }
        private async Task RunOneTestAsync(string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var singleTestRunner = scope.ServiceProvider.GetRequiredService<ISingleTestRunner>();
                await singleTestRunner.RunTestAsync(testRunId, testRunDirectory, testSuiteDefinition, browserConfig);
            }
        }
    }
}
