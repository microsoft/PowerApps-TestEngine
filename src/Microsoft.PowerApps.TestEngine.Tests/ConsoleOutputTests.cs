// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Users;
using Microsoft.PowerFx.Types;
using Moq;
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
            Assert.Contains("\n   App URL: make.powerapps.com/testapp", printer.ToString());
        }

        [Fact]
        public void TestSuiteEnd()
        {
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            _testEngineEventHandler._casesTotal = 11;
            _testEngineEventHandler._casesPassed = 6;

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
    }
}
