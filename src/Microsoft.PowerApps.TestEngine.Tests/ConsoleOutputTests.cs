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

        private void SetupMocks()
        {
            MockTestEngineEventHandler.Setup(x => x.SetNumberOfTotalCases(It.IsAny<int>()));
            MockTestEngineEventHandler.Setup(x => x.EncounteredException(It.IsAny<Exception>()));
            MockTestEngineEventHandler.Setup(x => x.SuiteBegin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            MockTestEngineEventHandler.Setup(x => x.SuiteEnd());
            MockTestEngineEventHandler.Setup(x => x.TestCaseBegin(It.IsAny<string>()));
            MockTestEngineEventHandler.Setup(x => x.TestCaseEnd(It.IsAny<bool>()));
        }

        [Fact]
        public void TestEncounteredException()
        {
            // Setup Mocks
            Mock<TestEngineEventHandler> MockTestEngineEventHandler = new Mock<TestEngineEventHandler>(MockBehavior.Strict);
            SetupMocks();
            
            // Specify expected result and output object
            var expected = "   Message";
            var printer = new StringWriter();
            Exception ex = new Exception("Message");

            // Set output
            Console.SetOut(printer);

            // Run function
            MockTestEngineEventHandler.EncounteredException(ex);

            // Assert that the expected output matches the console output of the function
            Assert.AreEqual(printer.ToString(), expected);
        }
        
        [Fact]
        public void TestSuiteBegin()
        {
            // Setup Mocks
            Mock<TestEngineEventHandler> MockTestEngineEventHandler = new Mock<TestEngineEventHandler>(MockBehavior.Strict);
            SetupMocks();
            
            // Specify expected result and output object
            var expected = $"\nRunning test suite: TestSuiteName";
            expected += $"\n   Test results will be stored in: ./testDirectory";
            expected += $"\n   Browser: Chromium";
            expected += $"\n   App URL: make.powerapps.com/testapp";

            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            MockTestEngineEventHandler.SuiteBegin("TestSuiteName", "./testDirectory", "Chromium", "make.powerapps.com/testapp&source=testengine");

            // Assert that the expected output matches the console output of the function
            Assert.AreEqual(printer.ToString(), expected);
        }

        [Fact]
        public void TestSuiteEnd()
        {
            // Setup Mocks
            Mock<TestEngineEventHandler> MockTestEngineEventHandler = new Mock<TestEngineEventHandler>(MockBehavior.Strict);
            SetupMocks();
            
            // Specify expected result and output object
            var expected = "\n\nTest suite summary";
            expected += $"Total cases: 11";
            expected += $"Cases passed: 6";
            expected += $"Cases failed: 5";
            
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            MockTestEngineEventHandler.SuiteEnd(11, 6, 5);

            // Assert that the expected output matches the console output of the function
            Assert.AreEqual(printer.ToString(), expected);
        }

        [Fact]
        public void TestTestCaseBegin()
        {
            // Setup Mocks
            Mock<TestEngineEventHandler> MockTestEngineEventHandler = new Mock<TestEngineEventHandler>(MockBehavior.Strict);
            SetupMocks();
            
            // Specify expected result and output object
            var expected = "\n Test case: MyCase";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            MockTestEngineEventHandler.TestCaseBegin("MyCase");

            // Assert that the expected output matches the console output of the function
            Assert.AreEqual(printer.ToString(), expected);
        }

        [Fact]
        public void TestTestCaseEndPassed()
        {
            // Setup Mocks
            Mock<TestEngineEventHandler> MockTestEngineEventHandler = new Mock<TestEngineEventHandler>(MockBehavior.Strict);
            SetupMocks();
            
            // Specify expected result and output object
            var expected = "\n   Result: Passed";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            MockTestEngineEventHandler.TestCaseEnd(true);

            // Assert that the expected output matches the console output of the function
            Assert.AreEqual(printer.ToString(), expected);
        }
        
        [Fact]
        public void TestTestCaseEndFailed()
        {
            // Setup Mocks
            Mock<TestEngineEventHandler> MockTestEngineEventHandler = new Mock<TestEngineEventHandler>(MockBehavior.Strict);
            SetupMocks();
            
            // Specify expected result and output object
            var expected = "\n   Result: Failed";
            var printer = new StringWriter();

            // Set output
            Console.SetOut(printer);

            // Run function
            MockTestEngineEventHandler.TestCaseEnd(false);

            // Assert that the expected output matches the console output of the function
            Assert.AreEqual(printer.ToString(), expected);
        }
    }
}
