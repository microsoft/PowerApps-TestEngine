// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
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

        public async Task<string> RunTestAsync(string testConfigFile, string environmentId, string tenantId, string outputDirectory, string domain, string queryParams)
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

            if (string.IsNullOrEmpty(queryParams))
            {
                Logger.LogDebug($"Using no additional query parameters.");
            }
            else
            {
                Logger.LogDebug($"Using query: {queryParams}");
            }
            Logger.LogDebug($"Using domain: {domain}");
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
                _state.SetDomain(domain);

                Logger.LogDebug($"Using domain: {domain}");

                await RunTestByWorkerCountAsync(testRunId, testRunDirectory, domain, queryParams);
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

        public async Task RunTestByWorkerCountAsync(string testRunId, string testRunDirectory, string domain, string queryParams)
        {
            var testSettings = _state.GetTestSettings();

            var locale = GetLocaleFromTestSettings(testSettings.Locale);

            var browserConfigurations = testSettings.BrowserConfigurations;
            var allTestRuns = new List<Task>();

            // Manage number of workers
            foreach (var browserConfig in browserConfigurations)
            {
                allTestRuns.Add(Task.Run(() => RunOneTestAsync(testRunId, testRunDirectory, _state.GetTestSuiteDefinition(), browserConfig, domain, queryParams, locale)));
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
        private async Task RunOneTestAsync(string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig, string domain, string queryParams, CultureInfo locale)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var singleTestRunner = scope.ServiceProvider.GetRequiredService<ISingleTestRunner>();
                await singleTestRunner.RunTestAsync(testRunId, testRunDirectory, testSuiteDefinition, browserConfig, domain, queryParams, locale);
            }
        }

        private CultureInfo GetLocaleFromTestSettings(string strLocale)
        {
            var locale = CultureInfo.CurrentCulture;
            try
            {
                if (string.IsNullOrEmpty(strLocale))
                {
                    Logger.LogInformation($"Locale unspecified in Test Suite Definition, using system locale {locale.Name}");
                }
                else
                {
                    locale = new CultureInfo(strLocale);
                    Logger.LogInformation($"Locale selected in Test Suite Definition: {locale.Name}");
                }
                if (!string.Equals(locale.Name, CultureInfo.CurrentCulture.Name))
                {
                    Logger.LogInformation($"Test Suite Locale ({locale.Name}) and system locale ({CultureInfo.CurrentCulture.Name}) do not match!");
                }
            }
            catch (ArgumentException)
            {
                Logger.LogError($"Locale from test suite definition {strLocale} unrecognized");
            }
            return locale;
        }
    }
}
