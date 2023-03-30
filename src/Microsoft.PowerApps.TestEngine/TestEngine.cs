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
    /// This class covers the core functionality of Test Engine, including the entry point for executing tests.
    /// </summary>
    public class TestEngine
    {
        private readonly ITestState _state;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestReporter _testReporter;
        private readonly IFileSystem _fileSystem;
        private readonly ILoggerFactory _loggerFactory;

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

        /// <summary>
        /// This is the entry point function for executing tests. This function expects the Test Plan file along with 
        /// environment and tenant IDs along with other optional parameters and executes the specified tests on the app
        /// in the provided tenant and environment.
        /// </summary>
        /// <param name="testConfigFile">The (absolute or relative) path to the test plan file.</param>
        /// <param name="environmentId">The environment ID where the Power App is published.</param>
        /// <param name="tenantId">The tenant ID where the Power App is published.</param>
        /// <param name="outputDirectory">The output directory where the test results and logs are to be saved.</param>
        /// <param name="domain">The domain of the Power Apps Canvas Designer application where the app is published (Example: "apps.powerapps.com").</param>
        /// <param name="queryParams">Optional query parameters that would be passed to the Player URL for optional features or parameters.</param>
        /// <returns>The full path where the test results are saved.</returns>
        /// <exception cref="ArgumentNullException">Throws ArgumentNullException if any of testConfigFile, environmentId, tenantId or domain are missing or empty.</exception>
        public async Task<string> RunTestAsync(FileInfo testConfigFile, string environmentId, Guid tenantId, DirectoryInfo outputDirectory, string domain, string queryParams)
        {
            // Set up test reporting
            var testRunId = _testReporter.CreateTestRun("Power Fx Test Runner", "User"); // TODO: determine if there are more meaningful values we can put here
            _testReporter.StartTestRun(testRunId);

            Logger = _loggerFactory.CreateLogger(testRunId);

            Logger.LogDebug($"Using output directory: {outputDirectory.FullName}");
            _state.SetOutputDirectory(outputDirectory.FullName);

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
                if (testConfigFile == null)
                {
                    Logger.LogError("testConfigFile cannot be null");
                    throw new ArgumentNullException(nameof(testConfigFile));
                }

                if (string.IsNullOrEmpty(environmentId))
                {
                    Logger.LogError("environmentId cannot be null");
                    throw new ArgumentNullException(nameof(environmentId));
                }

                if (tenantId == null || tenantId == Guid.Empty)
                {
                    Logger.LogError("tenantId cannot be null or empty");
                    throw new ArgumentNullException(nameof(tenantId));
                }

                if (string.IsNullOrEmpty(domain))
                {
                    Logger.LogError("domain cannot be null");
                    throw new ArgumentNullException(nameof(domain));
                }

                _state.ParseAndSetTestState(testConfigFile.FullName);
                _state.SetEnvironment(environmentId);
                _state.SetTenant(tenantId.ToString());
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

        private async Task RunTestByWorkerCountAsync(string testRunId, string testRunDirectory, string domain, string queryParams)
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
                    Logger.LogWarning($"Locale property not specified in testSettings. Using current system locale: {locale.Name}");
                }
                else
                {
                    locale = new CultureInfo(strLocale);
                    Logger.LogInformation($"Locale selected in Test Suite Definition: {locale.Name}");
                }
            }
            catch (ArgumentException)
            {
                Logger.LogError($"Locale from test suite definition {strLocale} unrecognized");
                throw;
            }
            return locale;
        }
    }
}
