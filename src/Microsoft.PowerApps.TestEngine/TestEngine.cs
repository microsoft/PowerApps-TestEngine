// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Reporting;

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

        public async Task<string> RunTestAsync(string testConfigFile, string environmentId, string tenantId, string outputDirectory = DefaultOutputDirectory, string cloud = DefaultCloud)
        {
            // Setup state
            if (string.IsNullOrEmpty(testConfigFile))
            {
                throw new ArgumentNullException(nameof(testConfigFile));
            }

            if (string.IsNullOrEmpty(environmentId))
            {
                throw new ArgumentNullException(nameof(environmentId));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            _state.ParseAndSetTestState(testConfigFile);
            _state.SetEnvironment(environmentId);
            _state.SetTenant(tenantId);

            if (string.IsNullOrEmpty(cloud))
            {
                _state.SetCloud(DefaultCloud);
            }
            else
            {
                _state.SetCloud(cloud);
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                _state.SetOutputDirectory(DefaultOutputDirectory);
            }
            else
            {
                _state.SetOutputDirectory(outputDirectory);
            }

            // Set up test reporting
            var testRunId = _testReporter.CreateTestRun("Power Fx Test Runner", "User"); // TODO: determine if there are more meaningful values we can put here
            _testReporter.StartTestRun(testRunId);
            var testRunDirectory = Path.Combine(_state.GetOutputDirectory(), testRunId.Substring(0, 6));
            _fileSystem.CreateDirectory(testRunDirectory);

            var browserConfigurations = _state.GetTestSettings().BrowserConfigurations;

            Queue<Task> allTestRuns = new Queue<Task>();

            // Manage number of workers
            foreach(var testDefinition in _state.GetTestDefinitions())
            {
                foreach (var browserConfig in browserConfigurations)
                {
                    allTestRuns.Enqueue(RunOneTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig));
                    if (allTestRuns.Count >= _state.GetWorkers())
                    {
                        var maxTestRuns = new List<Task>();
                        while (allTestRuns.Count > 0)
                        {
                            maxTestRuns.Add(allTestRuns.Dequeue());
                        }
                        await Task.WhenAll(maxTestRuns.ToArray());
                    }
                }
            }
            var restTestRuns = new List<Task>();
            while (allTestRuns.Count > 0)
            {
                restTestRuns.Add(allTestRuns.Dequeue());
            }
            await Task.WhenAll(restTestRuns.ToArray());

            _testReporter.EndTestRun(testRunId);
            return _testReporter.GenerateTestReport(testRunId, testRunDirectory);
        }

        private async Task RunOneTestAsync(string testRunId, string testRunDirectory, TestDefinition testDefinition, BrowserConfiguration browserConfig)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var singleTestRunner = scope.ServiceProvider.GetRequiredService<ISingleTestRunner>();
                await singleTestRunner.RunTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig);
            }
        }
    }
}
