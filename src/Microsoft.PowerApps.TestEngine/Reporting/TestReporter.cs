// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    public class TestReporter : ITestReporter
    {
        private readonly Dictionary<string, TestRun> _testRuns = new();
        private readonly IFileSystem _fileSystem;
        private readonly DateTime _defaultDateTime = new DateTime();

        public static string FailedResultOutcome = "Failed";
        public static string PassedResultOutcome = "Passed";
        public static string ResultsPrefix = "Results_";

        private string testRunAppURL;
        private string testResultsDirectory;
        public string TestRunAppURL { get => testRunAppURL; set => testRunAppURL = value; }
        public string TestResultsDirectory { get => testResultsDirectory; set => testResultsDirectory = value; }

        public TestReporter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

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
                    Counters = new TestCounters(),
                    Output = new TestOutput()
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
            var testRun = GetTestRun(testRunId);

            if (testRun.Times.Start != _defaultDateTime)
            {
                throw new InvalidOperationException("Test run has already started");
            }

            testRun.Times.Start = DateTime.Now;
        }

        public void EndTestRun(string testRunId)
        {
            var testRun = GetTestRun(testRunId);
            if (testRun.Times.Start == _defaultDateTime)
            {
                throw new InvalidOperationException("Test run has not been started");
            }

            if (testRun.Times.Finish != _defaultDateTime)
            {
                throw new InvalidOperationException("Can't end a test run is already finsihed");
            }

            testRun.Times.Finish = DateTime.Now;
            testRun.ResultSummary.Outcome = "Completed";

            // Update the ResultSummary Output
            _updateResultSummaryOutPut(testRun);
        }

        private void _updateResultSummaryOutPut(TestRun testRun)
        {
            var resultOutputMessage = $"{{ \"AppURL\": \"{testRunAppURL}\", \"TestResults\": \"{testResultsDirectory}\"}}";
            testRun.ResultSummary.Output.StdOut = resultOutputMessage;
        }

        public string CreateTestSuite(string testRunId, string testSuiteName)
        {
            var testRun = GetTestRun(testRunId);

            if (testRun.Times.Start == _defaultDateTime)
            {
                throw new InvalidOperationException("Test run needs to be started");
            }

            if (testRun.Times.Finish != _defaultDateTime)
            {
                throw new InvalidOperationException("Test run is already finished");
            }

            var testList = new TestList() { Id = Guid.NewGuid().ToString(), Name = testSuiteName };
            testRun.TestLists.TestList.Add(testList);
            return testList.Id;
        }

        private bool DoesTestSuiteExist(TestRun testRun, string testSuiteId)
        {
            return testRun.TestLists.TestList.Where(x => x.Id == testSuiteId).Count() == 1;
        }

        public string CreateTest(string testRunId, string testSuiteId, string testName)
        {
            var testRun = GetTestRun(testRunId);

            if (testRun.Times.Start == _defaultDateTime)
            {
                throw new InvalidOperationException("Test run needs to be started");
            }

            if (testRun.Times.Finish != _defaultDateTime)
            {
                throw new InvalidOperationException("Test run is already finished");
            }

            if (!DoesTestSuiteExist(testRun, testSuiteId))
            {
                throw new InvalidOperationException("Test suite needs to be created");
            }

            var unitTestDefinition = new UnitTestDefinition
            {
                Name = testName,
                Storage = $"{ResultsPrefix}{testRunId}",
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
                TestListId = testSuiteId
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

            testRun.Definitions.UnitTests.Add(unitTestDefinition);
            testRun.TestEntries.Entries.Add(testEntry);
            testRun.Results.UnitTestResults.Add(unitTestResult);
            testRun.ResultSummary.Counters.Total++;

            return unitTestDefinition.Id;
        }

        public void StartTest(string testRunId, string testId)
        {
            var testRun = GetTestRun(testRunId);
            var testResult = testRun.Results.UnitTestResults.Where(x => x.TestId == testId).FirstOrDefault();

            if (testResult == null)
            {
                throw new InvalidOperationException("Test id has to exist");
            }

            if (testResult.StartTime != _defaultDateTime)
            {
                throw new InvalidOperationException("Can't start a test that is already started");
            }

            testResult.StartTime = DateTime.Now;
            testRun.ResultSummary.Counters.InProgress++;
        }

        public void FailTest(string testRunId, string testId)
        {
            var testRun = GetTestRun(testRunId);
            var testResult = testRun.Results.UnitTestResults.Where(x => x.TestId == testId).First();
            testRun.ResultSummary.Counters.Failed++;
            testResult.Outcome = FailedResultOutcome;
        }

        public void EndTest(string testRunId, string testId, bool success, string stdout, List<string> additionalFiles, string errorMessage)
        {
            var testRun = GetTestRun(testRunId);
            var testResult = testRun.Results.UnitTestResults.Where(x => x.TestId == testId).First();

            if (testResult.StartTime == _defaultDateTime)
            {
                throw new InvalidOperationException("Can't end a test that isn't started");
            }

            if (testResult.EndTime != _defaultDateTime)
            {
                throw new InvalidOperationException("Can't end a test that is already finished");
            }

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
            testRun.ResultSummary.Counters.InProgress--;
            testRun.ResultSummary.Counters.Completed++;
            if (success)
            {
                testRun.ResultSummary.Counters.Passed++;
                testResult.Outcome = PassedResultOutcome;
            }
            else
            {
                testRun.ResultSummary.Counters.Failed++;
                testResult.Outcome = FailedResultOutcome;
                testResult.Output.ErrorInfo = new TestErrorInfo();
                testResult.Output.ErrorInfo.Message = errorMessage;
            }
        }

        public string GenerateTestReport(string testRunId, string resultsDirectory)
        {
            var testRun = GetTestRun(testRunId);
            XmlSerializerNamespaces xsNS = new XmlSerializerNamespaces();
            xsNS.Add("", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
            var serializer = new XmlSerializer(typeof(TestRun));
            var testResultPath = Path.Combine(resultsDirectory, $"{ResultsPrefix}{testRunId}.trx");
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun, xsNS);
                _fileSystem.WriteTextToFile(testResultPath, writer.ToString());
            }

            return testResultPath;
        }

        public TestRun GetTestRun(string testRunId)
        {
            if (string.IsNullOrEmpty(testRunId))
            {
                throw new ArgumentException(nameof(testRunId));
            }

            if (!_testRuns.ContainsKey(testRunId))
            {
                throw new ArgumentException(nameof(testRunId));
            }


            return _testRuns[testRunId];
        }
    }
}
