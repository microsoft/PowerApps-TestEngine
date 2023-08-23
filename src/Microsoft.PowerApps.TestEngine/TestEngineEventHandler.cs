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
        private int _casesTotal = 0;
        private int _casesPassed = 0;

        // NOTE: Any changes to these messages need to be handled in the consuming tool's console event handler, like in pac cli tool.
        // These console messages need to be considered for localization.
        public static string UserAppExceptionMessage = "   [Critical Error] Could not access PowerApps. For more details, check the logs.";
        public static string UserInputExceptionInvalidFilePathMessage = "   Invalid file path. For more details, check the logs.";
        public static string UserInputExceptionInvalidOutputPathMessage = "   [Critical Error]: The output directory provided is invalid.";
        public static string UserInputExceptionInvalidTestSettingsMessage = "   Invalid test settings specified in testconfig. For more details, check the logs.";
        public static string UserInputExceptionLoginCredentialMessage = "   Invalid login credential(s). For more details, check the logs.";
        public static string UserInputExceptionTestConfigMessage = "   Invalid test config. For more details, check the logs.";
        public static string UserInputExceptionYAMLFormatMessage = "   Invalid YAML format. For more details, check the logs.";

        public int CasesPassed { get => _casesPassed; set => _casesPassed = value; }
        public int CasesTotal { get => _casesTotal; set => _casesTotal = value; }

        public TestEngineEventHandler()
        {
        }

        public void SetAndInitializeCounters(int numCases)
        {
            _casesTotal = numCases;
            _casesPassed = 0;
        }

        public void EncounteredException(Exception ex)
        {
            // Print assertion if exception is the result of an Assert failure
            if (ex is AssertionFailureException)
            {
                Console.WriteLine($"   Assertion failed: {ex.InnerException.InnerException.Message}");
            }
            else if (ex is UserInputException)
            {
                switch (ex.Message)
                {
                    case nameof(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings):
                        Console.WriteLine(UserInputExceptionInvalidTestSettingsMessage);
                        break;
                    case nameof(UserInputException.ErrorMapping.UserInputExceptionInvalidFilePath):
                        Console.WriteLine(UserInputExceptionInvalidFilePathMessage);
                        break;
                    case nameof(UserInputException.ErrorMapping.UserInputExceptionLoginCredential):
                        Console.WriteLine(UserInputExceptionLoginCredentialMessage);
                        break;
                    case nameof(UserInputException.ErrorMapping.UserInputExceptionTestConfig):
                        Console.WriteLine(UserInputExceptionTestConfigMessage);
                        break;
                    case nameof(UserInputException.ErrorMapping.UserInputExceptionYAMLFormat):
                        Console.WriteLine(UserInputExceptionYAMLFormatMessage);
                        break;
                    case nameof(UserInputException.ErrorMapping.UserInputExceptionInvalidOutputPath):
                        Console.WriteLine(UserInputExceptionInvalidOutputPathMessage);
                        break;
                    default:
                        Console.WriteLine($"   {ex.Message}");
                        break;
                }
            }
            else if (ex is UserAppException)
            {
                Console.WriteLine(UserAppExceptionMessage);
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
            Console.WriteLine($"   App URL: {url}");
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
