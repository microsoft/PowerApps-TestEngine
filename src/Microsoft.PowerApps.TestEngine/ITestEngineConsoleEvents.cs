// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Interface that handles all console related events
    /// </summary>
    public interface ITestEngineEvents
    {
        /// <summary>
        /// Handles the assertion message when the Assert() function fails
        /// </summary>
        public void AssertionFailed(string assertion);

        /// <summary>
        /// Handles logging for an exception
        /// </summary>
        public void EncounteredException(Exception ex);

        /// <summary>
        /// Handles the starting suite output
        /// </summary>
        public void SuiteBegin(string suiteName, string directory, string browserName, string url);

        /// <summary>
        /// Handles the suite summary output
        /// </summary>
        public void SuiteEnd(int casesTotal, int casesPass, int casesFail);

        /// <summary>
        /// Handles logging for the suite name
        /// </summary>
        public void TestCaseName(string name);

        /// <summary>
        /// Handles result of a test case
        /// </summary>
        public void TestCasePassed(bool result);

        /// <summary>
        /// Handles the output for test report path
        /// </summary>
        public void TestReportPath(string path);
    }
}
