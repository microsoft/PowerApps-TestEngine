// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestRunSummaryTests
    {
        private Mock<IFileSystem> MockFileSystem;

        public TestRunSummaryTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("")]
        public void ThrowsOnInvalidResultsDirectoryTest(string resultsDirectory)
        {
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GenerateSummaryReport(resultsDirectory, "outputPath"));
        }
        
        [Fact]
        public void ThrowsOnNullResultsDirectoryTest()
        {
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GenerateSummaryReport(null, "outputPath"));
        }

        [Theory]
        [InlineData("")]
        public void ThrowsOnInvalidOutputPathTest(string outputPath)
        {
            var resultsDirectory = @"C:\TestResults";
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath));
        }
          [Fact]
        public void ThrowsOnNullOutputPathTest()
        {
            var resultsDirectory = @"C:\TestResults";
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GenerateSummaryReport(resultsDirectory, null));
        }

        [Fact]
        public void ThrowsOnNonExistentResultsDirectoryTest()
        {            var resultsDirectory = @"C:\NonExistentDirectory";
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(false);
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GenerateSummaryReport(resultsDirectory, "outputPath"));
        }

        [Fact]
        public void ThrowsOnNoTrxFilesFoundTest()
        {            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory, "*.trx")).Returns(Array.Empty<string>());
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<InvalidOperationException>(() => testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath));
        }
        
        [Fact]
        public void GetTrxFilesTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var trxFiles = new[] { @"C:\TestResults\file1.trx", @"C:\TestResults\file2.trx" };
            
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory, "*.trx")).Returns(trxFiles);
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var result = testRunSummary.GetTrxFiles(resultsDirectory);
            
            Assert.Equal(trxFiles, result);
        }
        
        [Theory]
        [InlineData("")]
        public void GetTrxFilesThrowsOnInvalidDirectoryTest(string directoryPath)
        {
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GetTrxFiles(directoryPath));
        }
        
        [Fact]
        public void GetTrxFilesThrowsOnNullDirectoryTest()
        {
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GetTrxFiles(null));
        }
        
        [Fact]
        public void GetTrxFilesThrowsOnNonExistentDirectoryTest()
        {
            var resultsDirectory = @"C:\NonExistentDirectory";
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(false);
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GetTrxFiles(resultsDirectory));
        }

        [Fact]
        public void GenerateSummaryReportTest()
        {            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";
            var trxFile1 = @"C:\TestResults\Results_1.trx";
            var trxFile2 = @"C:\TestResults\Results_2.trx";
            
            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory, "*.trx")).Returns(new[] { trxFile1, trxFile2 });
            
            // Setup mock directory for output
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));
            
            // Setup mock TRX file content
            var testRun1 = CreateTestRun("Test Run 1", "1", true);
            var testRun2 = CreateTestRun("Test Run 2", "2", false);
            
            // Serialize test runs to XML
            var serializer = new XmlSerializer(typeof(TestRun));
            string testRun1Xml, testRun2Xml;
            
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun1);
                testRun1Xml = writer.ToString();
            }
            
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun2);
                testRun2Xml = writer.ToString();
            }
            
            MockFileSystem.Setup(x => x.ReadAllText(trxFile1)).Returns(testRun1Xml);
            MockFileSystem.Setup(x => x.ReadAllText(trxFile2)).Returns(testRun2Xml);
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), false));
            
            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);
            
            // Verify the result
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public void GenerateSummaryReportHandlesInvalidTrxFilesTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";
            var validTrxFile = @"C:\TestResults\Valid.trx";
            var invalidTrxFile = @"C:\TestResults\Invalid.trx";
            var emptyTrxFile = @"C:\TestResults\Empty.trx";
            
            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory, "*.trx")).Returns(new[] { validTrxFile, invalidTrxFile, emptyTrxFile });
            
            // Setup mock directory for output
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));
            
            // Setup mock TRX file content
            var validTestRun = CreateTestRun("Valid Test Run", "1", true);
            var validTrxXml = SerializeTestRun(validTestRun);
            
            MockFileSystem.Setup(x => x.ReadAllText(validTrxFile)).Returns(validTrxXml);
            MockFileSystem.Setup(x => x.ReadAllText(invalidTrxFile)).Returns("<invalid>XML</invalid>");
            MockFileSystem.Setup(x => x.ReadAllText(emptyTrxFile)).Returns(string.Empty);
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), false));
            
            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);
            
            // Verify the result - it should still generate a report with the valid test run
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public void GenerateSummaryReportIncludesEnhancedStylesTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";
            var trxFile = @"C:\TestResults\Results.trx";
            
            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory, "*.trx")).Returns(new[] { trxFile });
            
            // Setup mock directory for output
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));
            
            // Create multiple test results with different outcomes
            var testRun = new TestRun
            {
                Name = "Test Run With Mixed Results",
                Id = "mixed-123",
                Times = new TestTimes
                {
                    Creation = DateTime.Now,
                    Queuing = DateTime.Now,
                    Start = DateTime.Now,
                    Finish = DateTime.Now.AddMinutes(5)
                },
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "PassingTest",
                            Outcome = TestReporter.PassedResultOutcome,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now.AddSeconds(10),
                            Duration = "00:00:10"
                        },
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "FailingTest",
                            Outcome = TestReporter.FailedResultOutcome,
                            StartTime = DateTime.Now.AddSeconds(15),
                            EndTime = DateTime.Now.AddSeconds(25),
                            Duration = "00:00:10",
                            Output = new TestOutput
                            {
                                ErrorInfo = new TestErrorInfo { Message = "Test failure with a very long error message that should get truncated in the display but still be available in the details section when the user clicks on Show More." }
                            }
                        },
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "OtherTest",
                            Outcome = "NotRun",
                            StartTime = DateTime.Now.AddSeconds(30),
                            EndTime = DateTime.Now.AddSeconds(35),
                            Duration = "00:00:05"
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Outcome = "Completed",
                    Output = new TestOutput
                    {
                        StdOut = "{ \"AppURL\": \"https://example.com/app\", \"TestResults\": \"C:\\\\Results\" }"
                    }
                }
            };
            
            var trxXml = SerializeTestRun(testRun);
            
            // Set up the mock file system to return our test run XML
            MockFileSystem.Setup(x => x.ReadAllText(trxFile)).Returns(trxXml);
            
            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), false))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);
            
            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);
            
            // Verify the result
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), false), Times.Once);
            
            // Check that the HTML includes our enhanced styling elements
            Assert.NotNull(capturedHtml);
            Assert.Contains("=\"header\"", capturedHtml);
            Assert.Contains("summary-stats", capturedHtml);
            Assert.Contains("summary-card success", capturedHtml);
            Assert.Contains("summary-card danger", capturedHtml);
            Assert.Contains("Powered by PowerApps Test Engine", capturedHtml);
        }

        private TestRun CreateTestRun(string name, string id, bool success)
        {
            var testRun = new TestRun
            {
                Name = name,
                Id = id,
                Times = new TestTimes
                {
                    Creation = DateTime.Now,
                    Queuing = DateTime.Now,
                    Start = DateTime.Now,
                    Finish = DateTime.Now.AddMinutes(1)
                },
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = $"Test_{id}",
                            Outcome = success ? TestReporter.PassedResultOutcome : TestReporter.FailedResultOutcome,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now.AddSeconds(30),
                            Duration = "00:00:30",
                            Output = new TestOutput
                            {
                                StdOut = "Test output",
                                ErrorInfo = success ? null : new TestErrorInfo { Message = "Error message" }
                            }
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Outcome = "Completed",
                    Counters = new TestCounters
                    {
                        Total = 1,
                        Executed = 1,
                        Passed = success ? 1 : 0,
                        Failed = success ? 0 : 1
                    },
                    Output = new TestOutput
                    {
                        StdOut = $"{{ \"AppURL\": \"https://example.com/{id}\", \"TestResults\": \"C:\\\\Results\\\\{id}\" }}"
                    }
                }
            };

            return testRun;
        }

        private string SerializeTestRun(TestRun testRun)
        {
            var serializer = new XmlSerializer(typeof(TestRun));
            using var writer = new StringWriter();
            serializer.Serialize(writer, testRun);
            return writer.ToString();
        }
    }
}
