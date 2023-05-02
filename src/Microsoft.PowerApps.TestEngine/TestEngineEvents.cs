// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Handles all major Test Engine events using console output
    /// </summary>
    public class TestEngineEvents : ITestEngineEvents
    {
        public void SetNumberOfTotalCases(int numCases)
        {
        }

        public void EncounteredException(Exception ex)
        {
        }

        public void SuiteBegin(string suiteName, string directory, string browserName, string url)
        {
        }

        public void SuiteEnd()
        {
        }

        public void TestCaseBegin(string name)
        {
        }

        public void TestCaseEnd(bool result)
        {
        }
    }
}
