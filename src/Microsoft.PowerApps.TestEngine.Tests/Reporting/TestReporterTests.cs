// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestReporterTests
    {
        private Mock<IFileSystem> MockFileSystem;
        private readonly DateTime DefaultDateTime = new DateTime();

        public TestReporterTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nonexistentid")]
        public void ThrowsOnInvalidTestRunIdTest(string testRunId)
        {
            var testReporter = new TestReporter(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testReporter.GetTestRun(testRunId));
            Assert.Throws<ArgumentException>(() => testReporter.StartTestRun(testRunId));
            Assert.Throws<ArgumentException>(() => testReporter.EndTestRun(testRunId));
            Assert.Throws<ArgumentException>(() => testReporter.CreateTest(testRunId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => testReporter.StartTest(testRunId, Guid.NewGuid().ToString()));
            Assert.Throws<ArgumentException>(() => testReporter.EndTest(testRunId, Guid.NewGuid().ToString(), true, "", new List<string>(), null));
            Assert.Throws<ArgumentException>(() => testReporter.GenerateTestReport(testRunId, "c:\\results"));
        }

        [Fact]
        public void CreateTestRunTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var before = DateTime.Now;
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);
            var after = DateTime.Now;
            var testRun = testReporter.GetTestRun(testRunId);

            Assert.NotNull(testRun);
            Assert.Equal(testRunName, testRun.Name);
            Assert.Equal(testUser, testRun.RunUser);
            Assert.Equal(testRunId, testRun.Id);
            Assert.True(before <= testRun.Times.Creation && testRun.Times.Creation <= after);
            Assert.True(before <= testRun.Times.Queuing && testRun.Times.Queuing <= after);
            Assert.Equal(DefaultDateTime, testRun.Times.Start);
            Assert.Equal(DefaultDateTime, testRun.Times.Finish);
            Assert.True(testRun.Results.UnitTestResults.Count == 0);
            Assert.True(testRun.Definitions.UnitTests.Count == 0);
            Assert.True(testRun.TestEntries.Entries.Count == 0);
            Assert.True(testRun.ResultSummary.Outcome == "");
            Assert.True(testRun.ResultSummary.Counters.Total == 0);
            Assert.True(testRun.ResultSummary.Counters.Executed == 0);
            Assert.True(testRun.ResultSummary.Counters.Passed == 0);
            Assert.True(testRun.ResultSummary.Counters.Failed == 0);
            Assert.True(testRun.ResultSummary.Counters.Error == 0);
            Assert.True(testRun.ResultSummary.Counters.Timeout == 0);
            Assert.True(testRun.ResultSummary.Counters.Aborted == 0);
            Assert.True(testRun.ResultSummary.Counters.Inconclusive == 0);
            Assert.True(testRun.ResultSummary.Counters.PassedButRunAborted == 0);
            Assert.True(testRun.ResultSummary.Counters.NotRunnable == 0);
            Assert.True(testRun.ResultSummary.Counters.NotExecuted == 0);
            Assert.True(testRun.ResultSummary.Counters.Disconnected == 0);
            Assert.True(testRun.ResultSummary.Counters.Warning == 0);
            Assert.True(testRun.ResultSummary.Counters.Completed == 0);
            Assert.True(testRun.ResultSummary.Counters.InProgress == 0);
            Assert.True(testRun.ResultSummary.Counters.Inconclusive == 0);
            Assert.True(testRun.ResultSummary.Counters.Pending == 0);
            Assert.Equal("8c84fa94-04c1-424b-9868-57a2d4851a1d", testRun.TestLists.TestList[0].Id);
            Assert.Equal("Results Not in a List", testRun.TestLists.TestList[0].Name);
            Assert.Equal("19431567-8539-422a-85d7-44ee4e166bda", testRun.TestLists.TestList[1].Id);
            Assert.Equal("Results Not in a List", testRun.TestLists.TestList[1].Name);
        }

        [Fact]
        public void StartTestRunTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);

            var before = DateTime.Now;
            testReporter.StartTestRun(testRunId);
            var after = DateTime.Now;

            var testRun = testReporter.GetTestRun(testRunId);
            Assert.True(before <= testRun.Times.Start && testRun.Times.Start <= after);

            Assert.Throws<InvalidOperationException>(() => testReporter.StartTestRun(testRunId));
        }

        [Fact]
        public void EndTestRunTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);

            Assert.Throws<InvalidOperationException>(() => testReporter.EndTestRun(testRunId));

            testReporter.StartTestRun(testRunId);

            var before = DateTime.Now;
            testReporter.EndTestRun(testRunId);
            var after = DateTime.Now;

            var testRun = testReporter.GetTestRun(testRunId);
            Assert.True(before <= testRun.Times.Finish && testRun.Times.Finish <= after);
            Assert.Equal("Completed", testRun.ResultSummary.Outcome);

            Assert.Throws<InvalidOperationException>(() => testReporter.EndTestRun(testRunId));
            Assert.Equal("{ \"AppURL\": \"\", \"TestResults\": \"\"}", testRun.ResultSummary.Output.StdOut);
        }

        [Fact]
        public void CreateTestSuiteTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testSuiteName = "testSuite";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);

            Assert.Throws<InvalidOperationException>(() => testReporter.CreateTestSuite(testRunId, testSuiteName));

            testReporter.StartTestRun(testRunId);

            var testSuiteId = testReporter.CreateTestSuite(testRunId, testSuiteName);

            Assert.NotNull(testSuiteId);

            var testRun = testReporter.GetTestRun(testRunId);
            var testList = testRun.TestLists.TestList.Where(x => x.Id == testSuiteId);
            Assert.Single(testList);
            Assert.Equal(testSuiteName, testList.First().Name);

            testReporter.EndTestRun(testRunId);
            Assert.Throws<InvalidOperationException>(() => testReporter.CreateTestSuite(testRunId, testSuiteName));
        }

        [Fact]
        public void CreateTestTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testName = "testName";
            var testSuiteName = "testSuite";            
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);
            var testLocation = $"{TestReporter.ResultsPrefix}{testRunId}";

            Assert.Throws<InvalidOperationException>(() => testReporter.CreateTest(testRunId, Guid.NewGuid().ToString(), testName));

            testReporter.StartTestRun(testRunId);

            Assert.Throws<InvalidOperationException>(() => testReporter.CreateTest(testRunId, Guid.NewGuid().ToString(), testName));

            var testSuiteId = testReporter.CreateTestSuite(testRunId, testSuiteName);

            var testId = testReporter.CreateTest(testRunId, testSuiteId, testName);

            var testRun = testReporter.GetTestRun(testRunId);

            Assert.Equal(testName, testRun.Definitions.UnitTests[0].Name);
            Assert.Equal(testLocation, testRun.Definitions.UnitTests[0].Storage);
            Assert.Equal(testId, testRun.Definitions.UnitTests[0].Id);
            Assert.True(!string.IsNullOrEmpty(testRun.Definitions.UnitTests[0].Execution.Id));
            Assert.Equal("", testRun.Definitions.UnitTests[0].Method.CodeBase);
            Assert.Equal("powerfx-test-runner", testRun.Definitions.UnitTests[0].Method.AdapterTypeName);
            Assert.Equal("PowerFXTests", testRun.Definitions.UnitTests[0].Method.ClassName);
            Assert.Equal(testName, testRun.Definitions.UnitTests[0].Method.Name);

            Assert.Equal(testId, testRun.TestEntries.Entries[0].TestId);
            Assert.Equal(testRun.Definitions.UnitTests[0].Execution.Id, testRun.TestEntries.Entries[0].ExecutionId);
            Assert.Equal(testSuiteId, testRun.TestEntries.Entries[0].TestListId);

            Assert.Equal(testRun.Definitions.UnitTests[0].Execution.Id, testRun.Results.UnitTestResults[0].ExecutionId);
            Assert.Equal(testId, testRun.Results.UnitTestResults[0].TestId);
            Assert.Equal(testName, testRun.Results.UnitTestResults[0].TestName);
            Assert.Equal("", testRun.Results.UnitTestResults[0].ComputerName);
            Assert.Equal("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b", testRun.Results.UnitTestResults[0].TestType);
            Assert.Equal(testSuiteId, testRun.Results.UnitTestResults[0].TestListId);
            Assert.NotNull(testRun.Results.UnitTestResults[0].Output);
            Assert.Equal(DefaultDateTime, testRun.Results.UnitTestResults[0].StartTime);
            Assert.Equal(DefaultDateTime, testRun.Results.UnitTestResults[0].EndTime);
            Assert.True(testRun.Results.UnitTestResults[0].ResultFiles.ResultFile.Count == 0);
            Assert.Equal(1, testRun.ResultSummary.Counters.Total);

            var testName2 = "testName2";
            var testLocation2 = $"{TestReporter.ResultsPrefix}{testRunId}";
            var testId2 = testReporter.CreateTest(testRunId, testSuiteId, testName2);

            testRun = testReporter.GetTestRun(testRunId);

            Assert.Equal(testName2, testRun.Definitions.UnitTests[1].Name);
            Assert.Equal(testLocation2, testRun.Definitions.UnitTests[1].Storage);
            Assert.Equal(testId2, testRun.Definitions.UnitTests[1].Id);
            Assert.True(!string.IsNullOrEmpty(testRun.Definitions.UnitTests[1].Execution.Id));
            Assert.Equal("", testRun.Definitions.UnitTests[1].Method.CodeBase);
            Assert.Equal("powerfx-test-runner", testRun.Definitions.UnitTests[1].Method.AdapterTypeName);
            Assert.Equal("PowerFXTests", testRun.Definitions.UnitTests[1].Method.ClassName);
            Assert.Equal(testName2, testRun.Definitions.UnitTests[1].Method.Name);

            Assert.Equal(testId2, testRun.TestEntries.Entries[1].TestId);
            Assert.Equal(testRun.Definitions.UnitTests[1].Execution.Id, testRun.TestEntries.Entries[1].ExecutionId);
            Assert.Equal(testSuiteId, testRun.TestEntries.Entries[1].TestListId);

            Assert.Equal(testRun.Definitions.UnitTests[1].Execution.Id, testRun.Results.UnitTestResults[1].ExecutionId);
            Assert.Equal(testId2, testRun.Results.UnitTestResults[1].TestId);
            Assert.Equal(testName2, testRun.Results.UnitTestResults[1].TestName);
            Assert.Equal("", testRun.Results.UnitTestResults[1].ComputerName);
            Assert.Equal("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b", testRun.Results.UnitTestResults[1].TestType);
            Assert.Equal(testSuiteId, testRun.Results.UnitTestResults[1].TestListId);
            Assert.NotNull(testRun.Results.UnitTestResults[1].Output);
            Assert.Equal(DefaultDateTime, testRun.Results.UnitTestResults[0].StartTime);
            Assert.Equal(DefaultDateTime, testRun.Results.UnitTestResults[0].EndTime);
            Assert.True(testRun.Results.UnitTestResults[1].ResultFiles.ResultFile.Count == 0);
            Assert.Equal(2, testRun.ResultSummary.Counters.Total);

            testReporter.EndTestRun(testRunId);
            Assert.Throws<InvalidOperationException>(() => testReporter.CreateTest(testRunId, Guid.NewGuid().ToString(), testName));
        }

        [Fact]
        public void CreateTestStorageNameTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testName = "testName";
            var testSuiteName = "testSuite";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);
            testReporter.StartTestRun(testRunId);
            var testSuiteId = testReporter.CreateTestSuite(testRunId, testSuiteName);
            var testId = testReporter.CreateTest(testRunId, testSuiteId, testName);
            var testRun = testReporter.GetTestRun(testRunId);

            Assert.Equal($"{TestReporter.ResultsPrefix}{testRunId}", testRun.Definitions.UnitTests[0].Storage);
            Assert.Equal(testId, testRun.Definitions.UnitTests[0].Id);
        }

        [Fact]
        public void StartTestTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testName = "testName";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);

            testReporter.StartTestRun(testRunId);

            var testSuiteId = testReporter.CreateTestSuite(testRunId, "testSuite");
            var testId = testReporter.CreateTest(testRunId, testSuiteId, testName);

            var before = DateTime.Now;
            testReporter.StartTest(testRunId, testId);
            var after = DateTime.Now;

            var testRun = testReporter.GetTestRun(testRunId);

            Assert.True(before < testRun.Results.UnitTestResults[0].StartTime && testRun.Results.UnitTestResults[0].StartTime < after);
            Assert.Equal(1, testRun.ResultSummary.Counters.InProgress);

            Assert.Throws<InvalidOperationException>(() => testReporter.StartTest(testRunId, testId));
            Assert.Throws<InvalidOperationException>(() => testReporter.StartTest(testRunId, Guid.NewGuid().ToString()));
        }

        [Theory]
        [InlineData(true, "some logs", new string[] { }, null)]
        [InlineData(true, "some logs", new string[] { "file1.txt", "file2.txt", "file3.txt" }, null)]
        [InlineData(false, "some logs", new string[] { }, null)]
        [InlineData(true, "some logs", new string[] { }, "error message")]
        public void EndTestTest(bool success, string stdout, string[] additionalFiles, string errorMessage)
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testName = "testName";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);

            testReporter.StartTestRun(testRunId);

            var testSuiteId = testReporter.CreateTestSuite(testRunId, "testSuite");
            var testId = testReporter.CreateTest(testRunId, testSuiteId, testName);

            Assert.Throws<InvalidOperationException>(() => testReporter.EndTest(testRunId, testId, success, stdout, additionalFiles.ToList(), errorMessage));

            testReporter.StartTest(testRunId, testId);

            var before = DateTime.Now;
            testReporter.EndTest(testRunId, testId, success, stdout, additionalFiles.ToList(), errorMessage);
            var after = DateTime.Now;

            var testRun = testReporter.GetTestRun(testRunId);

            Assert.True(before < testRun.Results.UnitTestResults[0].EndTime && testRun.Results.UnitTestResults[0].EndTime < after);
            Assert.Equal((testRun.Results.UnitTestResults[0].EndTime - testRun.Results.UnitTestResults[0].StartTime).ToString(), testRun.Results.UnitTestResults[0].Duration);
            Assert.Equal(stdout, testRun.Results.UnitTestResults[0].Output.StdOut);

            Assert.Equal(additionalFiles.Length, testRun.Results.UnitTestResults[0].ResultFiles.ResultFile.Count);

            foreach (var file in additionalFiles)
            {
                Assert.True(testRun.Results.UnitTestResults[0].ResultFiles.ResultFile.Where(x => x.Path == file).FirstOrDefault() != null);
            }

            Assert.Equal(0, testRun.ResultSummary.Counters.InProgress);
            Assert.Equal(1, testRun.ResultSummary.Counters.Completed);

            if (success)
            {
                Assert.Equal(1, testRun.ResultSummary.Counters.Passed);
                Assert.Equal(0, testRun.ResultSummary.Counters.Failed);
                Assert.Equal(TestReporter.PassedResultOutcome, testRun.Results.UnitTestResults[0].Outcome);
                Assert.Null(testRun.Results.UnitTestResults[0].Output.ErrorInfo);
            }
            else
            {
                Assert.Equal(0, testRun.ResultSummary.Counters.Passed);
                Assert.Equal(1, testRun.ResultSummary.Counters.Failed);
                Assert.Equal(TestReporter.FailedResultOutcome, testRun.Results.UnitTestResults[0].Outcome);
                Assert.Equal(errorMessage, testRun.Results.UnitTestResults[0].Output.ErrorInfo.Message);
            }

            Assert.Throws<InvalidOperationException>(() => testReporter.EndTest(testRunId, testId, success, stdout, additionalFiles.ToList(), errorMessage));
        }

        [Fact]
        public void FailTestTest()
        {
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testName = "testName";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);

            testReporter.StartTestRun(testRunId);

            var testSuiteId = testReporter.CreateTestSuite(testRunId, "testSuite");
            var testId = testReporter.CreateTest(testRunId, testSuiteId, testName);

            testReporter.FailTest(testRunId, testId);

            var testRun = testReporter.GetTestRun(testRunId);
            var testResult = testRun.Results.UnitTestResults.Where(x => x.TestId == testId).First();

            Assert.True(testResult.Outcome == TestReporter.FailedResultOutcome);
            Assert.Equal(1, testRun.ResultSummary.Counters.Failed);
        }

        [Fact]
        public void GenerateTestReportTest()
        {
            bool success = true;
            string stdout = "some logs";
            List<string> additionalFiles = new List<string>() { "file1.txt" };
            var testRunName = "testRunName";
            var testUser = "testUser";
            var testName = "testName";
            var resultDirectory = "C:\\results";
            var testReporter = new TestReporter(MockFileSystem.Object);
            var testRunId = testReporter.CreateTestRun(testRunName, testUser);
            testReporter.TestRunAppURL = "someAppURL";
            testReporter.TestResultsDirectory = "someResultsDirectory";

            testReporter.StartTestRun(testRunId);

            var testSuiteId = testReporter.CreateTestSuite(testRunId, "testSuite");
            var testId = testReporter.CreateTest(testRunId, testSuiteId, testName);

            testReporter.StartTest(testRunId, testId);

            testReporter.EndTest(testRunId, testId, success, stdout, additionalFiles, null);

            testReporter.EndTestRun(testRunId);

            MockFileSystem.Setup(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>()));

            var trxPath = testReporter.GenerateTestReport(testRunId, resultDirectory);

            var expectedTrxPath = Path.Combine(resultDirectory, $"Results_{testRunId}.trx");
            Assert.Equal(Path.Combine(resultDirectory, $"Results_{testRunId}.trx"), trxPath);
            var validateTestResults = (string serializedTestResults) =>
            {
                if (!serializedTestResults.Contains("http://microsoft.com/schemas/VisualStudio/TeamTest/2010"))
                {
                    return false;
                }

                var serializer = new XmlSerializer(typeof(TestRun));
                var reader = XmlReader.Create(new StringReader(serializedTestResults));
                var deserializedTestRun = (TestRun)serializer.Deserialize(reader);
                var testRun = testReporter.GetTestRun(testRunId);

                Assert.Equal(JsonConvert.SerializeObject(testRun), JsonConvert.SerializeObject(deserializedTestRun));

                // test the setting of result summary
                Assert.Equal("{ \"AppURL\": \"someAppURL\", \"TestResults\": \"someResultsDirectory\"}", testRun.ResultSummary.Output.StdOut);
                return true;
            };
            MockFileSystem.Verify(x => x.WriteTextToFile(expectedTrxPath, It.Is<string>(y => validateTestResults(y))), Times.Once());
        }
    }  
}
