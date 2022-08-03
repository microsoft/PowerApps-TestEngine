// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting
{
    public class TestReporter : ITestReporter
    {
        private readonly Dictionary<string, TestRun> _testRuns = new();
        private readonly IFileSystem _fileSystem;
        private readonly DateTime _defaultDateTime = new DateTime();

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

        public void StartTestRun(string testRunId, ILogger logger)
        {
            var testRun = GetTestRun(testRunId, logger);

            if (testRun.Times.Start != _defaultDateTime)
            {
                logger.LogError("Test run has already started");
            }

            testRun.Times.Start = DateTime.Now;
        }

        public void EndTestRun(string testRunId, ILogger logger)
        {
            var testRun = GetTestRun(testRunId, logger);
            if (testRun.Times.Start == _defaultDateTime)
            {
                logger.LogError("Test run has not been started");
            }

            if (testRun.Times.Finish != _defaultDateTime)
            {
                logger.LogError("Can't end a test run is already finsihed");
            }

            testRun.Times.Finish = DateTime.Now;
            testRun.ResultSummary.Outcome = "Completed";
        }

        public string CreateTest(string testRunId, string testName, string testLocation, ILogger logger)
        {
            var testRun = GetTestRun(testRunId, logger);

            if (testRun.Times.Start == _defaultDateTime)
            {
                logger.LogError("Test run needs to be started");
            }

            if (testRun.Times.Finish != _defaultDateTime)
            {
                logger.LogError("Test run is already finished");
            }

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
                TestListId = testRun.TestLists.TestList.First().Id
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

        public void StartTest(string testRunId, string testId, ILogger logger)
        {
            var testRun = GetTestRun(testRunId, logger);
            var testResult = testRun.Results.UnitTestResults.Where(x => x.TestId == testId).FirstOrDefault();

            if (testResult == null)
            {
                logger.LogError("Test id has to exist");
            }

            if (testResult.StartTime != _defaultDateTime)
            {
                logger.LogError("Can't start a test that is already started");
            }

            testResult.StartTime = DateTime.Now;
            testRun.ResultSummary.Counters.InProgress++;
        }

        public void EndTest(string testRunId, string testId, bool success, string stdout, List<string> additionalFiles, string? errorMessage, string? stackTrace, ILogger logger)
        {
            var testRun = GetTestRun(testRunId, logger);
            var testResult = testRun.Results.UnitTestResults.Where(x => x.TestId == testId).First();

            if (testResult.StartTime == _defaultDateTime)
            {
                logger.LogError("Can't end a test that isn't started");
            }

            if (testResult.EndTime != _defaultDateTime)
            {
                logger.LogError("Can't end a test that is already finished");
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
                testResult.Outcome = "Passed";
            }
            else
            {
                testRun.ResultSummary.Counters.Failed++;
                testResult.Outcome = "Failed";
                testResult.Output.ErrorInfo = new TestErrorInfo();
                testResult.Output.ErrorInfo.Message = errorMessage;
                testResult.Output.ErrorInfo.StackTrace = stackTrace;
            }
        }

        public string GenerateTestReport(string testRunId, string resultsDirectory, ILogger logger)
        {
            var testRun = GetTestRun(testRunId, logger);
            XmlSerializerNamespaces xsNS = new XmlSerializerNamespaces();
            xsNS.Add("", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
            var serializer = new XmlSerializer(typeof(TestRun));
            var testResultPath = Path.Combine(resultsDirectory, $"Results_{testRunId}.trx");
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun, xsNS);
                _fileSystem.WriteTextToFile(testResultPath, writer.ToString());
            }

            return testResultPath;
        }

        public TestRun GetTestRun(string testRunId, ILogger logger)
        {
            if (string.IsNullOrEmpty(testRunId))
            {
                logger.LogTrace("Test run id: " + nameof(testRunId));
                logger.LogError("Test run id cannot be null nor empty.");
            }

            if (!_testRuns.ContainsKey(testRunId))
            {
                logger.LogTrace("Test run id: " + nameof(testRunId));
                logger.LogError("Test run id does not exist.");
            }

            return _testRuns[testRunId];
        }
    }
}
