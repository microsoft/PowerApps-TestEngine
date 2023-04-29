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
        public void AssertionFailed(string assertion)
        {
            Console.Write("Assertion failed: " + assertion);
        }

        public void EncounteredException(Exception ex)
        {
            Console.Write("Encountered exception: " + ex);
        }

        public void SuiteBegin(string suiteName, string directory, string browserName, string url)
        {
            Console.Write("Suite began: " + suiteName);
        }

        public void SuiteEnd()
        {
            Console.Write("Suite ended");
        }

        public void TestCaseBegin(string name)
        {
            Console.Write("Test case began: " + name);
        }

        public void TestCaseEnd(bool result)
        {
            Console.Write("Test case ended. Passed: " + result);
        }

        public void TestReportPath(string path)
        {
            Console.Write("Test report path: " + path);
        }
    }
}
