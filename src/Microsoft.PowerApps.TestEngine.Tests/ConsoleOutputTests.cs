// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests
{
    public class ConsoleOutputTests
    {
        TestEngineEventHandler _testEngineEventHandler;

        public ConsoleOutputTests()
        {
            _testEngineEventHandler = new TestEngineEventHandler();
        }

        [Fact]
        public void TestEncounteredException()
        {
            // Specify expected result and output object
            var expected = "   Message";
            var printer = new StringWriter();
            Exception ex = new Exception("Message");

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.EncounteredException(ex);

            // Assert that the expected output matches the console output of the function
            Assert.Contains(expected, printer.ToString());
        }

        [Fact]
        public void TestSuiteBegin()
        {
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.SuiteBegin("TestSuiteName", "./testDirectory", "Chromium", "make.powerapps.com/testapp&source=testengine");

            // Assert that the expected output matches the console output of the function
            Assert.Contains("Running test suite: TestSuiteName", printer.ToString());
            Assert.Contains("\n   Test results will be stored in: ./testDirectory", printer.ToString());
            Assert.Contains("\n   Browser: Chromium", printer.ToString());
            Assert.Contains("\n   App URL: make.powerapps.com/testapp&source=testengine", printer.ToString());
        }

        [Fact]
        public void TestSuiteEnd()
        {
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            _testEngineEventHandler.CasesTotal = 11;
            _testEngineEventHandler.CasesPassed = 6;

            // Run function
            _testEngineEventHandler.SuiteEnd();

            // Assert that the expected output matches the console output of the function
            Assert.Contains("\nTest suite summary", printer.ToString());
            Assert.Contains("Total cases: 11", printer.ToString());
            Assert.Contains("Cases passed: 6", printer.ToString());
            Assert.Contains("Cases failed: 5", printer.ToString());
        }

        [Fact]
        public void TestTestCaseBegin()
        {
            // Specify expected result and output object
            var expected = "Test case: MyCase";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.TestCaseBegin("MyCase");

            // Assert that the expected output matches the console output of the function
            Assert.Contains(expected, printer.ToString());
        }

        [Fact]
        public void TestTestCaseEndPassed()
        {
            // Specify expected result and output object
            var expected = "   Result: Passed";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.TestCaseEnd(true);

            // Assert that the expected output matches the console output of the function
            Assert.Contains(expected, printer.ToString());
        }

        [Fact]
        public void TestTestCaseEndFailed()
        {
            // Specify expected result and output object
            var expected = "   Result: Failed";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.TestCaseEnd(false);

            // Assert that the expected output matches the console output of the function
            Assert.Contains(expected, printer.ToString());
        }

        [Fact]
        public void TestMultipleBrowserRuns()
        {
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run tests on first Browser
            _testEngineEventHandler.SetAndInitializeCounters(2);
            _testEngineEventHandler.SuiteBegin("TestSuiteName", "./testDirectory", "Chromium", "make.powerapps.com/testapp&source=testengine");
            _testEngineEventHandler.TestCaseBegin("Case1");
            _testEngineEventHandler.TestCaseEnd(true);
            _testEngineEventHandler.TestCaseBegin("Case2");
            _testEngineEventHandler.TestCaseEnd(false);
            _testEngineEventHandler.SuiteEnd();

            // Run tests on second Browser
            _testEngineEventHandler.SetAndInitializeCounters(2);
            _testEngineEventHandler.SuiteBegin("TestSuiteName", "./testDirectory", "Firefox", "make.powerapps.com/testapp&source=testengine");
            _testEngineEventHandler.TestCaseBegin("Case1");
            _testEngineEventHandler.TestCaseEnd(true);
            _testEngineEventHandler.TestCaseBegin("Case2");
            _testEngineEventHandler.TestCaseEnd(false);
            _testEngineEventHandler.SuiteEnd();

            // Assert that the expected console output matches 
            Assert.Contains("\nTest suite summary", printer.ToString());
            Assert.Contains("Total cases: 2", printer.ToString());
            Assert.Contains("Cases passed: 1", printer.ToString());
            Assert.Contains("Cases failed: 1", printer.ToString());

            // Assert none of the browser runs failed to show incorrect failed cases
            Assert.DoesNotContain("Cases failed: 0", printer.ToString());
        }

        [Fact]
        public void TestEncounteredUserAppException()
        {
            // Specify expected result and output object
            var expected = TestEngineEventHandler.UserAppExceptionMessage;
            var printer = new StringWriter();
            Exception ex = new UserAppException();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.EncounteredException(ex);

            // Assert that the expected output matches the console output
            Assert.Contains(expected, printer.ToString());
        }

        [Theory]
        [ClassData(typeof(UserInputExceptionDataGenerator))]
        public void TestEncounteredUserInputException(string exceptionName, string expectedMessage)
        {
            // Specify expected result and output object
            var printer = new StringWriter();
            Exception ex = new UserInputException(exceptionName);

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.EncounteredException(ex);

            // Assert that the expected output matches the console output
            Assert.Contains(expectedMessage, printer.ToString());
        }

        class UserInputExceptionDataGenerator : TheoryData<string, string>
        {
            public UserInputExceptionDataGenerator()
            {
                Add(nameof(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings), TestEngineEventHandler.UserInputExceptionInvalidTestSettingsMessage);
                Add(nameof(UserInputException.ErrorMapping.UserInputExceptionInvalidOutputPath), TestEngineEventHandler.UserInputExceptionInvalidOutputPathMessage);
                Add(nameof(UserInputException.ErrorMapping.UserInputExceptionInvalidFilePath), TestEngineEventHandler.UserInputExceptionInvalidFilePathMessage);
                Add(nameof(UserInputException.ErrorMapping.UserInputExceptionLoginCredential), TestEngineEventHandler.UserInputExceptionLoginCredentialMessage);
                Add(nameof(UserInputException.ErrorMapping.UserInputExceptionTestConfig), TestEngineEventHandler.UserInputExceptionTestConfigMessage);
                Add(nameof(UserInputException.ErrorMapping.UserInputExceptionYAMLFormat), TestEngineEventHandler.UserInputExceptionYAMLFormatMessage);
            }
        }
    }
}
