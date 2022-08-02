// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.Extensions.Logging;

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
        private const string DefaultOutputDirectory = "TestOutput";
        private const string DefaultCloud = "Prod";

        public TestEngine(ITestState state,
                          IServiceProvider serviceProvider,
                          ITestReporter testReporter, 
                          IFileSystem fileSystem)
        {
            _state = state;
            _serviceProvider = serviceProvider;
            _testReporter = testReporter;
            _fileSystem = fileSystem;
        }

        public async Task<string> RunTestAsync(string testConfigFile, string environmentId, string tenantId, ILogger logger, string outputDirectory = DefaultOutputDirectory, string cloud = DefaultCloud)
        {
            // Setup state
            if (string.IsNullOrEmpty(testConfigFile))
            {
                logger.LogCritical("Test Config File cannot be null nor empty.");
                logger.LogTrace("Test Config File: " + nameof(testConfigFile));
            }

            if (string.IsNullOrEmpty(environmentId))
            {
                logger.LogCritical("Environment ID cannot be null nor empty.");
                logger.LogTrace("Environment ID: " + nameof(environmentId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                logger.LogCritical("Tenant ID cannot be null nor empty.");
                logger.LogTrace("Tenant ID: " + nameof(tenantId));
            }

            _state.ParseAndSetTestState(testConfigFile, logger);
            _state.SetEnvironment(environmentId, logger);
            _state.SetTenant(tenantId, logger);

            if (string.IsNullOrEmpty(cloud))
            {
                _state.SetCloud(DefaultCloud, logger);
            }
            else
            {
                _state.SetCloud(cloud, logger);
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                _state.SetOutputDirectory(DefaultOutputDirectory, logger);
            }
            else
            {
                _state.SetOutputDirectory(outputDirectory, logger);
            }

            // Set up test reporting
            var testRunId = _testReporter.CreateTestRun("Power Fx Test Runner", "User"); // TODO: determine if there are more meaningful values we can put here
            _testReporter.StartTestRun(testRunId, logger);
            var testRunDirectory = Path.Combine(_state.GetOutputDirectory(), testRunId.Substring(0, 6));
            _fileSystem.CreateDirectory(testRunDirectory);

            await RunTestByWorkerCountAsync(testRunId, testRunDirectory);

            _testReporter.EndTestRun(testRunId, logger);
            return _testReporter.GenerateTestReport(testRunId, testRunDirectory, logger);
        }

        public async Task RunTestByWorkerCountAsync(string testRunId, string testRunDirectory)
        {
            var browserConfigurations = _state.GetTestSettings().BrowserConfigurations;
            var allTestRuns = new List<Task>();

            // Manage number of workers
            foreach (var browserConfig in browserConfigurations)
            {
                allTestRuns.Add(RunOneTestAsync(testRunId, testRunDirectory, _state.GetTestSuiteDefinition(), browserConfig));
                if (allTestRuns.Count >= _state.GetWorkerCount())
                {
                    await Task.WhenAll(allTestRuns.ToArray());
                    allTestRuns.Clear();
                }
            }
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
