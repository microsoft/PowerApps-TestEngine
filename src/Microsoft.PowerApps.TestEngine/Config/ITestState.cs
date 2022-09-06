// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// State of the test run
    /// </summary>
    public interface ITestState
    {
        /// <summary>
        /// Parses and sets up the test state.
        /// </summary>
        /// <param name="testConfigFile">Config file for test</param>
        public void ParseAndSetTestState(string testConfigFile);

        /// <summary>
        /// Gets the test suite defined for the test run.
        /// </summary>
        /// <returns>The test suite for test</returns>
        public TestSuiteDefinition GetTestSuiteDefinition();

        /// <summary>
        /// Gets all the test cases defined for the test run.
        /// </summary>
        /// <returns>List of test cases</returns>
        public List<TestCase> GetTestCases();

        /// <summary>
        /// Set the environment id the apps should be opened in.
        /// </summary>
        /// <param name="environmentId">Environment id</param>
        public void SetEnvironment(string environmentId);

        /// <summary>
        /// Gets the environment id the apps should be opened in.
        /// </summary>
        /// <returns>Environment id</returns>
        public string GetEnvironment();

        /// <summary>
        /// Set the cloud the app should be opened in.
        /// </summary>
        /// <param name="cloud">Cloud</param>
        public void SetCloud(string cloud);

        /// <summary>
        /// Gets the cloud the app should be opened in.
        /// </summary>
        /// <returns>Cloud</returns>
        public string GetCloud();

        /// <summary>
        /// Sets the tenant the app should be opened in.
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        public void SetTenant(string tenantId);

        /// <summary>
        /// Gets the tenant the app should be opened in.
        /// </summary>
        /// <returns>Tenant id</returns>
        public string GetTenant();

        /// <summary>
        /// Sets the directory that all test outputs should be placed in.
        /// </summary>
        /// <param name="outputDirectory">Output directory</param>
        public void SetOutputDirectory(string outputDirectory);

        /// <summary>
        /// Gets the directory that all tests outputs should be placed in.
        /// </summary>
        /// <returns>Output directory</returns>
        public string GetOutputDirectory();

        /// <summary>
        /// Gets the user configuration given a persona.
        /// </summary>
        /// <param name="persona">Persona</param>
        /// <returns>User configuration</returns>
        public UserConfiguration GetUserConfiguration(string persona);

        /// <summary>
        /// Gets the test settings.
        /// </summary>
        /// <returns>Gets the test settings</returns>
        public TestSettings GetTestSettings();

        /// <summary>
        /// Gets the timeout value from the test settings.
        /// </summary>
        /// <returns>The timeout value</returns>
        public int GetTimeout();

        /// <summary>
        /// Gets the number of workers from the test settings.
        /// </summary>
        /// <returns>Number of workers</returns>
        public int GetWorkerCount();

        /// <summary>
        /// Gets whether we are using TestEngine's JS implementation or not
        /// </summary>
        /// <returns>True/false if using TE JS implementation</returns>
        public bool UsingTestEngineJS();
    }
}
