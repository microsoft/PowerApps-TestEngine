// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Reporting.Format;
using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    public class TestReporter : ITestReporter
    {
        private readonly Dictionary<string, TestRun> _testRuns = new();

        public string CreateTestRun(string testRunName, string testRunUser)
        {
            var testRun = new TestRun
            {
                Name = testRunName,
                RunUser = testRunUser,
                Id = Guid.NewGuid().ToString(),
                Times = new TestTimes()
                {
                    Creation = DateTime.Now,
                    Queuing = DateTime.Now
                },
                Results = new TestResults()
                {
                    UnitTestResults = new List<UnitTestResult>()
                },
                Definitions = new TestDefinitions()
                {
                    UnitTests = new List<UnitTestDefinition>()
                },
                TestEntries = new TestEntries()
                {
                    Entries = new List<TestEntry>()
                },
                ResultSummary = new TestResultSummary()
                {
                    Outcome = "",
                    Counters = new TestCounters()
                },
                TestLists = new TestLists()
                {
                    TestList = new List<TestList>()
                }
            };

            var testList = new TestList()
            {
                // This needs to be hard coded
                Id = "8c84fa94-04c1-424b-9868-57a2d4851a1d",
                Name = "Results Not in a List"
            };
            testRun.TestLists.TestList.Add(testList);

            var testList2 = new TestList()
            {
                // This needs to be hard coded
                Id = "19431567-8539-422a-85d7-44ee4e166bda",
                Name = "Results Not in a List"
            };
            testRun.TestLists.TestList.Add(testList2);

            _testRuns.Add(testRun.Id, testRun);

            return testRun.Id;
        }

        public void StartTestRun(string testRunId)
        {
            _testRuns[testRunId].Times.Start = DateTime.Now;
        }

        public void EndTestRun(string testRunId)
        {
            _testRuns[testRunId].Times.Finish = DateTime.Now;
            _testRuns[testRunId].ResultSummary.Outcome = "Completed";
        }

        public string CreateTest(string testRunId, string testName, string testLocation)
        {
            var unitTestDefinition = new UnitTestDefinition
            {
                Name = testName,
                Storage = testLocation,
                Id = Guid.NewGuid().ToString(),
                Execution = new TestExecution()
                {
                    Id = Guid.NewGuid().ToString()
                },
                Method = new TestMethod()
                {
                    CodeBase = "",
                    AdapterTypeName = "powerfx-test-runner",
                    ClassName = "PowerFXTests",
                    Name = testName
                }
            };

            var testEntry = new TestEntry
            {
                TestId = unitTestDefinition.Id,
                ExecutionId = unitTestDefinition.Execution.Id,
                TestListId = _testRuns[testRunId].TestLists.TestList.First().Id
            };

            var unitTestResult = new UnitTestResult
            {
                ExecutionId = unitTestDefinition.Execution.Id,
                TestId = unitTestDefinition.Id,
                TestName = testName,
                ComputerName = "",
                // TestType has to be hardcoded to this for visual studio to open it
                TestType = "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b",
                TestListId = testEntry.TestListId,
                RelativeResultsDirectory = unitTestDefinition.Id,
                Output = new TestOutput(),
                ResultFiles = new TestResultFiles()
                {
                    ResultFile = new List<ResultFile>()
                }
            };

            _testRuns[testRunId].Definitions.UnitTests.Add(unitTestDefinition);
            _testRuns[testRunId].TestEntries.Entries.Add(testEntry);
            _testRuns[testRunId].Results.UnitTestResults.Add(unitTestResult);
            _testRuns[testRunId].ResultSummary.Counters.Total++;

            return unitTestDefinition.Id;
        }

        public void StartTest(string testRunId, string testId)
        {
            var testResult = _testRuns[testRunId].Results.UnitTestResults.Where(x => x.TestId == testId).First();
            testResult.StartTime = DateTime.Now;
            _testRuns[testRunId].ResultSummary.Counters.InProgress++;
        }

        public void EndTest(string testRunId, string testId, bool success, string stdout, List<string> additionalFiles, string errorMessage, string stackTrace)
        {
            var testResult = _testRuns[testRunId].Results.UnitTestResults.Where(x => x.TestId == testId).First();
            testResult.EndTime = DateTime.Now;
            testResult.Duration = (testResult.EndTime - testResult.StartTime).ToString();
            testResult.Output.StdOut = stdout;
            if (additionalFiles != null)
            {
                foreach (var additionalFile in additionalFiles)
                {
                    testResult.ResultFiles.ResultFile.Add(new ResultFile() { Path = additionalFile });
                }
            }
            _testRuns[testRunId].ResultSummary.Counters.InProgress--;
            _testRuns[testRunId].ResultSummary.Counters.Completed++;
            if (success)
            {
                _testRuns[testRunId].ResultSummary.Counters.Passed++;
                testResult.Outcome = "Passed";
            }
            else
            {
                _testRuns[testRunId].ResultSummary.Counters.Failed++;
                testResult.Outcome = "Failed";
                testResult.Output.ErrorInfo = new TestErrorInfo();
                testResult.Output.ErrorInfo.Message = errorMessage;
                testResult.Output.ErrorInfo.StackTrace = stackTrace;
            }
        }

        public string GenerateTestReport(string testRunId, string resultsDirectory)
        {
            XmlSerializerNamespaces xsNS = new XmlSerializerNamespaces();
            xsNS.Add("", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
            var serializer = new XmlSerializer(typeof(TestRun));
            var testResultPath = $"{resultsDirectory}/Results_{testRunId}.trx";
            using (StreamWriter writer = new StreamWriter(testResultPath))
            {
                serializer.Serialize(writer, _testRuns[testRunId], xsNS);
            }

            return testResultPath;
        }
    }
}
