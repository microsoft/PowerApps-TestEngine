// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Triggers for every major Test Engine event
    /// </summary>
    public interface ITestEngineEvents
    {
        /// <summary>
        /// Sets number of total cases to expect and resets number of passed cases to 0
        /// </summary>
        public void SetAndInitializeCounters(int numCases);

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
        public void SuiteEnd();

        /// <summary>
        /// Handles the starting case output
        /// </summary>
        public void TestCaseBegin(string name);

        /// <summary>
        /// Handles result of a test case
        /// </summary>
        public void TestCaseEnd(bool result);
    }
}
