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
        /// <param name="logger">Logger</param>
        public void ParseAndSetTestState(string testConfigFile, ILogger logger);

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
        /// Set the domain of the URL where the app will be launched
        /// </summary>
        /// <param name="domain">Domain</param>
        public void SetDomain(string domain);

        /// <summary>
        /// Gets the domain of the URL where the app will be launched
        /// </summary>
        /// <returns>Domain</returns>
        public string GetDomain();

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
    }
}
