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
    {   TestEngineEventHandler _testEngineEventHandler;

        public ConsoleOutputTests() {
            _testEngineEventHandler = new TestEngineEventHandler();
        }


        [Fact]
        public void TestEncounteredException()
        {
            // Specify expected result and output object
            var expected = "   Message\r\n";
            var printer = new StringWriter();
            Exception ex = new Exception("Message");

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.EncounteredException(ex);

            // Assert that the expected output matches the console output of the function
            Assert.Equal(expected, printer.ToString());
        }
        
        [Fact]
        public void TestSuiteBegin()
        {
            // Specify expected result and output object
            var expected = $"Running test suite: TestSuiteName\r";
            expected += $"\n   Test results will be stored in: ./testDirectory\r";
            expected += $"\n   Browser: Chromium\r";
            expected += $"\n   App URL: make.powerapps.com/testapp\r\n";

            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.SuiteBegin("TestSuiteName", "./testDirectory", "Chromium", "make.powerapps.com/testapp&source=testengine");

            // Assert that the expected output matches the console output of the function
            Assert.Equal(expected, printer.ToString());
        }

        [Fact]
        public void TestSuiteEnd()
        {
            // Specify expected result and output object
            var expected = "\nTest suite summary\r\n";
            expected += $"Total cases: 11\r\n";
            expected += $"Cases passed: 6\r\n";
            expected += $"Cases failed: 5\r\n";
            
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            _testEngineEventHandler._casesTotal = 11;
            _testEngineEventHandler._casesPassed = 6;

            // Run function
            _testEngineEventHandler.SuiteEnd();

            // Assert that the expected output matches the console output of the function
            Assert.Equal(expected, printer.ToString());
        }

        [Fact]
        public void TestTestCaseBegin()
        {
            // Specify expected result and output object
            var expected = "Test case: MyCase\r\n";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.TestCaseBegin("MyCase");

            // Assert that the expected output matches the console output of the function
            Assert.Equal(expected, printer.ToString());
        }

        [Fact]
        public void TestTestCaseEndPassed()
        {
             // Specify expected result and output object
            var expected = "   Result: Passed\r\n";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.TestCaseEnd(true);

            // Assert that the expected output matches the console output of the function
            Assert.Equal(expected, printer.ToString());
        }
        
        [Fact]
        public void TestTestCaseEndFailed()
        {
            // Specify expected result and output object
            var expected = "   Result: Failed\r\n";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            _testEngineEventHandler.TestCaseEnd(false);

            // Assert that the expected output matches the console output of the function
            Assert.Equal(expected, printer.ToString());
        }
    }
}
