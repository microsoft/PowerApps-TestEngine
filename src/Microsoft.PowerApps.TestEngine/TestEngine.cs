// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerApps.TestEngine.Config;
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
        private const string DefaultOutputDirectory = "TestOutput";
        private const string DefaultCloud = "Prod";

        public TestEngine(ITestState state,
                          IServiceProvider serviceProvider,
                          ITestReporter testReporter)
        {
            _state = state;
            _serviceProvider = serviceProvider;
            _testReporter = testReporter;
        }

        public async Task RunTestAsync(string testConfigFile, string environmentId, string tenantId, string outputDirectory = DefaultOutputDirectory, string cloud = DefaultCloud)
        {
            // Setup state
            _state.ParseAndSetTestState(testConfigFile);
            _state.SetEnvironment(environmentId);
            _state.SetTenant(tenantId);
            _state.SetCloud(cloud);
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
            var testRunDirectory = $"{_state.GetOutputDirectory()}/{testRunId.Substring(0, 6)}";
            Directory.CreateDirectory(testRunDirectory);

            var browserConfigurations = _state.GetTestSettings().BrowserConfigurations;

            var allTestRuns = new List<Task>();

            // TODO: manage number of workers
            foreach(var testDefinition in _state.GetTestDefinitions())
            {
                foreach (var browserConfig in browserConfigurations)
                {
                    allTestRuns.Add(RunOneTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig));
                }
            }

            await Task.WhenAll(allTestRuns.ToArray());

            _testReporter.EndTestRun(testRunId);
            _testReporter.GenerateTestReport(testRunId, testRunDirectory);
        }

        public async Task RunOneTestAsync(string testRunId, string testRunDirectory, TestDefinition testDefinition, BrowserConfiguration browserConfig)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var singleTestRunner = scope.ServiceProvider.GetRequiredService<ISingleTestRunner>();
                await singleTestRunner.RunTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig);
            }
        }
    }
}
