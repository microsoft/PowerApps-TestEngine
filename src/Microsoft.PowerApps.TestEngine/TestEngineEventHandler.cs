// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Handles all major Test Engine events using console output
    /// </summary>
    public class TestEngineEventHandler : ITestEngineEvents
    {
        public int _casesTotal = 0;
        public int _casesPassed = 0;

        public TestEngineEventHandler()
        {
        }

        public void SetNumberOfTotalCases(int numCases)
        {
            _casesTotal = numCases;
        }

        public void EncounteredException(Exception ex)
        {
            // Print assertion if exception is the result of an Assert failure
            if (ex is AssertionFailureException)
            {
                Console.WriteLine($"   Assertion failed: {ex.InnerException.InnerException.Message}");
            }
            else
            {
                Console.WriteLine($"   {ex.Message}");
            }
        }

        public void SuiteBegin(string suiteName, string directory, string browserName, string url)
        {
            Console.WriteLine($"Running test suite: {suiteName}");
            Console.WriteLine($"   Test results will be stored in: {directory}");
            Console.WriteLine($"   Browser: {browserName}");
            Console.WriteLine($"   App URL: {url.Replace("&source=testengine", string.Empty)}");
        }

        public void SuiteEnd()
        {
            Console.WriteLine("\nTest suite summary");
            Console.WriteLine($"Total cases: {_casesTotal}");
            Console.WriteLine($"Cases passed: {_casesPassed}");
            Console.WriteLine($"Cases failed: {(_casesTotal - _casesPassed)}");
        }

        public void TestCaseBegin(string name)
        {
            Console.WriteLine($"Test case: {name}");
        }

        public void TestCaseEnd(bool result)
        {
            if (result)
            {
                _casesPassed++;
                Console.WriteLine("   Result: Passed");
            }
            else
            {
                Console.WriteLine("   Result: Failed");
            }
        }
    }
}
