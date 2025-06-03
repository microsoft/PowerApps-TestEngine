// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        {
            var resultsDirectory = @"C:\NonExistentDirectory";
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(false);

            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testRunSummary.GenerateSummaryReport(resultsDirectory, "outputPath"));
        }        [Fact]
        public void GenerateSummaryReportTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";
            var trxFile1 = @"C:\TestResults\Results_1.trx";
            var trxFile2 = @"C:\TestResults\Results_2.trx";

            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);

            // Setup mock directory for output
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));

            // Setup mock TRX file content
            var testRun1 = CreateDummyTestRun("Test Run 1", true);
            var testRun2 = CreateDummyTestRun("Test Run 2", false);
            testRun1.Name = "Test Run 1";
            testRun2.Name = "Test Run 2";

            // Serialize test runs to XML
            string testRun1Xml = SerializeTestRun(testRun1);
            string testRun2Xml = SerializeTestRun(testRun2);

            MockFileSystem.Setup(x => x.ReadAllText(trxFile1)).Returns(testRun1Xml);
            MockFileSystem.Setup(x => x.ReadAllText(trxFile2)).Returns(testRun2Xml);
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true));
            MockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("<html>{{TEMPLATE}}</html>");

            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) => new[] { trxFile1, trxFile2 };
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Verify the result
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true), Times.Once);
        }        [Fact]
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

            // Setup mock directory for output
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));

            // Setup mock TRX file content
            var validTestRun = CreateDummyTestRun("Valid Test Run", true);
            validTestRun.Name = "Valid Test Run";
            var validTrxXml = SerializeTestRun(validTestRun);

            MockFileSystem.Setup(x => x.ReadAllText(validTrxFile)).Returns(validTrxXml);
            MockFileSystem.Setup(x => x.ReadAllText(invalidTrxFile)).Returns("<invalid>XML</invalid>");
            MockFileSystem.Setup(x => x.ReadAllText(emptyTrxFile)).Returns(string.Empty);
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true));
            MockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("<html>{{TEMPLATE}}</html>");

            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) =>
            {
                // Include all three files to test handling of invalid and empty files
                return new[] { validTrxFile, invalidTrxFile, emptyTrxFile };
            };
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Verify the result - it should still generate a report with the valid test run
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true), Times.Once);
        }        [Fact]
        public void GenerateSummaryReportIncludesEnhancedStylesTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";
            var trxFile = @"C:\TestResults\Results.trx";

            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);

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

            // Set up the mock file system to return our test run XML and HTML template
            MockFileSystem.Setup(x => x.ReadAllText(trxFile)).Returns(trxXml);
            MockFileSystem.Setup(x => x.ReadAllText(It.Is<string>(s => !s.Equals(trxFile))))
                .Returns(@"<html>
                    <head class=""header""></head>
                    <body>
                        <div class=""summary-stats"">
                            <div class=""summary-card success""></div>
                            <div class=""summary-card danger""></div>
                        </div>
                        <footer>Powered by PowerApps Test Engine</footer>
                    </body>
                </html>");

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) =>
            {
                // Simulate that it returns the valid TRX file only
                return new[] { trxFile };
            };
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Verify the result
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true), Times.Once);

            // Check that the HTML includes our enhanced styling elements
            Assert.NotNull(capturedHtml);
            Assert.Contains("=\"header\"", capturedHtml);
            Assert.Contains("summary-stats", capturedHtml);
            Assert.Contains("summary-card success", capturedHtml);
            Assert.Contains("summary-card danger", capturedHtml);
            Assert.Contains("Powered by PowerApps Test Engine", capturedHtml);
        }
        [Fact]
        public void GenerateHtmlReportParsesAppUrlsCorrectly()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";

            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));

            // Setup a list of test runs with different app URL formats
            var testRuns = new List<TestRun>
            {
                CreateTestRunWithUrl(
                    "Model-Driven App Standard Entity",
                    "1",
                    "https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account"
                ),
                CreateTestRunWithUrl(
                    "Model-Driven App Custom Page",
                    "2",
                    "https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=custompage"
                ),
                CreateTestRunWithUrl(
                    "Canvas App",
                    "3",
                    "https://apps.powerapps.com/play/e/default-tenant/a/1234abcd-5678-efgh-ijkl-9012mnop3456"
                ),
                CreateTestRunWithUrl(
                    "Power Apps Portal",
                    "4",
                    "https://make.powerapps.com/environments/Default-tenant/apps"
                ),
                CreateTestRunWithUrl(
                    "Power Apps Preview Portal",
                    "5",
                    "https://make.preview.powerapps.com/environments/Default-tenant/apps/view"
                )
            };

            // Set up file system mocks to return our test run XMLs
            for (int i = 0; i < testRuns.Count; i++)
            {
                var filePath = $@"C:\TestResults\Results_{i + 1}.trx";
                var xml = SerializeTestRun(testRuns[i]);
                MockFileSystem.Setup(x => x.ReadAllText(filePath)).Returns(xml);
            }

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Execute the method
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) =>
            {
                // Return our test file paths
                var files = new string[testRuns.Count];
                for (int i = 0; i < testRuns.Count; i++)
                {
                    files[i] = $@"C:\TestResults\Results_{i + 1}.trx";
                }
                return files;
            };

            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Verify the result was generated
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true), Times.Once);

            // Verify the HTML content contains our expected app types and page types
            Assert.NotNull(capturedHtml);
            Assert.Contains("Model-driven App", capturedHtml);
            Assert.Contains("entitylist", capturedHtml);
            Assert.Contains("account", capturedHtml);
            Assert.Contains("Custom Page", capturedHtml);
            Assert.Contains("custompage", capturedHtml);
            Assert.Contains("Canvas App", capturedHtml);
            Assert.Contains("Power Apps Portal", capturedHtml);
        }

        [Fact]
        public void GenerateHtmlReportContainsDifferentAppTypesTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\output\summary.html";
            var outputDirectory = @"C:\output";

            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));

            // Create the test runs with different app URLs
            var testRun1 = CreateTestRunWithUrl("Model-Driven App", "1", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account");
            var testRun2 = CreateTestRunWithUrl("Canvas App", "2", "https://apps.powerapps.com/play/e/default-tenant/a/1234abcd");
            var testRun3 = CreateTestRunWithUrl("Portal", "3", "https://make.powerapps.com/environments/Default-tenant/apps");

            // Set up mock file system to return the test run XML
            var trxFile1 = @"C:\TestResults\Results_1.trx";
            var trxFile2 = @"C:\TestResults\Results_2.trx";
            var trxFile3 = @"C:\TestResults\Results_3.trx";

            MockFileSystem.Setup(x => x.ReadAllText(trxFile1)).Returns(SerializeTestRun(testRun1));
            MockFileSystem.Setup(x => x.ReadAllText(trxFile2)).Returns(SerializeTestRun(testRun2));
            MockFileSystem.Setup(x => x.ReadAllText(trxFile3)).Returns(SerializeTestRun(testRun3));

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Create the test run summary instance and configure it to use our mock TRX files
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) =>
            {
                return new[] { trxFile1, trxFile2, trxFile3 };
            };

            // Generate the summary report
            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Verify the result was generated
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true), Times.Once);

            // Verify the HTML content contains our expected app types and page types
            Assert.NotNull(capturedHtml);

            // Check for model-driven app details
            Assert.Contains("Model-driven App", capturedHtml);  // App type
            Assert.Contains("entitylist", capturedHtml);        // Page type
            Assert.Contains("account", capturedHtml);           // Entity name

            // Check for canvas app
            Assert.Contains("Canvas App", capturedHtml);

            // Check for portal app
            Assert.Contains("Power Apps Portal", capturedHtml);
        }

        // Helper method to create test runs with specific app URLs
        private TestRun CreateTestRunWithUrl(string name, string id, string appUrl)
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
                            TestId = $"test-{id}",
                            TestName = $"Test {name}",
                            Outcome = TestReporter.PassedResultOutcome,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now.AddSeconds(5),
                            Duration = "00:00:05"
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
                        Passed = 1,
                        Failed = 0
                    },
                    Output = new TestOutput
                    {
                        StdOut = $"{{ \"AppURL\": \"{appUrl}\", \"TestResults\": \"C:\\\\Results\\\\{id}\" }}"
                    }
                }
            };

            return testRun;
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

        // Helper method to create dummy test runs
        private TestRun CreateDummyTestRun(string testName, bool passed)
        {
            var testRun = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Run",
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
                            TestName = testName,
                            Outcome = passed ? TestReporter.PassedResultOutcome : TestReporter.FailedResultOutcome,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now.AddSeconds(10),
                            Duration = "00:00:10"
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
                        Passed = passed ? 1 : 0,
                        Failed = passed ? 0 : 1
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

        [Theory]
        [InlineData("https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account", "Model-driven App", "entitylist", "account")]
        [InlineData("https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=custompage", "Model-driven App", "Custom Page", "custompage")]
        [InlineData("https://contoso.crm4.dynamics.com/main.aspx?pagetype=entity&etn=contact", "Model-driven App", "entity", "contact")]
        [InlineData("https://apps.powerapps.com/play/e/default-tenant/a/1234abcd", "Canvas App", "Unknown", "Unknown")]
        [InlineData("https://make.powerapps.com/environments/Default-tenant/apps", "Power Apps Portal", "environments", "apps")]
        [InlineData("https://make.preview.powerapps.com/environments/Default-tenant/solutions", "Power Apps Portal", "environments", "solutions")]
        [InlineData("https://invalid-url", "Unknown", "Unknown", "Unknown")]
        public void TestAppUrlParsing(string url, string expectedAppType, string expectedPageType, string expectedEntityName)
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var testRunSummary = new TestRunSummary(mockFileSystem.Object);

            // Act
            var result = testRunSummary.GetAppTypeAndEntityFromUrl(url);

            // Use reflection to get the tuple values
            var resultType = result.GetType();
            var appType = resultType.GetField("Item1").GetValue(result) as string;
            var pageType = resultType.GetField("Item2").GetValue(result) as string;
            var entityName = resultType.GetField("Item3").GetValue(result) as string;

            // Assert
            Assert.Equal(expectedAppType, appType);
            Assert.Equal(expectedPageType, pageType);
            Assert.Equal(expectedEntityName, entityName);
        }

        [Fact]
        public void LoadTestRunFromFile_ValidFile_ReturnsTestRun()
        {
            // Arrange
            var trxFilePath = @"C:\TestResults\valid.trx";
            var testRun = CreateDummyTestRun("test1", true);
            var serializedTestRun = SerializeTestRun(testRun);

            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(serializedTestRun);
            MockFileSystem.Setup(x => x.GetFileSize(It.IsAny<string>())).Returns(100000); // Mock file size for videos

            var testRunSummary = new TestRunSummary(MockFileSystem.Object);

            // Act
            var result = testRunSummary.LoadTestRunFromFile(trxFilePath);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.NotNull(result.Results.UnitTestResults);
            Assert.Single(result.Results.UnitTestResults);
            Assert.Equal("test1", result.Results.UnitTestResults[0].TestName);
            Assert.Equal("Passed", result.Results.UnitTestResults[0].Outcome);
        }

        [Fact]
        public void LoadTestRunFromFile_EmptyFile_ReturnsNull()
        {
            // Arrange
            var trxFilePath = @"C:\TestResults\empty.trx";
            
            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(string.Empty);
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);

            // Act
            var result = testRunSummary.LoadTestRunFromFile(trxFilePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LoadTestRunFromFile_InvalidXml_ReturnsNull()
        {
            // Arrange
            var trxFilePath = @"C:\TestResults\invalid.trx";
            
            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns("<invalid-xml>");
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);

            // Act
            var result = testRunSummary.LoadTestRunFromFile(trxFilePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void LoadTestRunFromFile_ValidFileWithVideos_EnhancesTestRunWithVideos()
        {
            // Arrange
            var trxFilePath = @"C:\TestResults\with-videos.trx";
            var testRun = CreateDummyTestRun("test-with-video", true);
            var serializedTestRun = SerializeTestRun(testRun);
            var videoFile = @"C:\TestResults\recording.webm";

            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(serializedTestRun);
            MockFileSystem.Setup(x => x.GetFileSize(videoFile)).Returns(100000); // Mock file size for videos
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            
            // Override the video file discovery function with a mock
            testRunSummary.GetVideoFiles = (path) => new string[] { videoFile };

            // Act
            var result = testRunSummary.LoadTestRunFromFile(trxFilePath);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ResultSummary);
            Assert.NotNull(result.ResultSummary.Output);
            Assert.Contains("VideoPath", result.ResultSummary.Output.StdOut);
            Assert.Contains("Videos", result.ResultSummary.Output.StdOut);
        }

        [Fact]
        public void GetTemplateContext_EmptyTestRuns_ReturnsDefaultTemplateData()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>();

            // Act
            var result = testRunSummary.GetTemplateContext(testRuns);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalTests);
            Assert.Equal(0, result.PassedTests);
            Assert.Equal(0, result.FailedTests);
            Assert.Equal(0, result.HealthScore);
            Assert.Equal("N/A", result.TotalDuration);
            Assert.Equal("N/A", result.StartTime);
            Assert.Equal("N/A", result.EndTime);
            Assert.NotNull(result.TestsData);
            Assert.NotEmpty(result.TestsData);
        }

        [Fact]
        public void GetTemplateContext_WithTestRuns_CalculatesCorrectSummary()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            
            // Create test runs with different outcomes
            var testRun1 = CreateDummyTestRun("test1", true);
            var testRun2 = CreateDummyTestRun("test2", false);
            
            // Set start and end times for test runs
            testRun1.Results.UnitTestResults[0].StartTime = DateTime.Now.AddMinutes(-10);
            testRun1.Results.UnitTestResults[0].EndTime = DateTime.Now.AddMinutes(-5);
            testRun1.Results.UnitTestResults[0].Duration = "00:05:00";
            
            testRun2.Results.UnitTestResults[0].StartTime = DateTime.Now.AddMinutes(-5);
            testRun2.Results.UnitTestResults[0].EndTime = DateTime.Now;
            testRun2.Results.UnitTestResults[0].Duration = "00:05:00";

            // Create a list with the test runs
            var testRuns = new List<TestRun> { testRun1, testRun2 };

            // Act
            var result = testRunSummary.GetTemplateContext(testRuns);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalTests);
            Assert.Equal(1, result.PassedTests);
            Assert.Equal(1, result.FailedTests);
            Assert.Equal(50, result.PassPercentage);
            Assert.NotEmpty(result.TotalDuration); // Should be calculated
            Assert.NotEqual("N/A", result.StartTime);
            Assert.NotEqual("N/A", result.EndTime);
            Assert.NotNull(result.TestsData);
            Assert.NotEmpty(result.TestsData);
        }

        [Fact]
        public void GenerateHtmlReport_EmptyTestRuns_GeneratesBaselineReport()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>();

            // Act
            var result = testRunSummary.GenerateHtmlReport(testRuns);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("<html", result); // Should contain the baseline HTML
        }

        [Fact]
        public void GenerateHtmlReport_WithTestRuns_GeneratesReportWithTestData()
        {
            // Arrange
            MockFileSystem.Setup(x => x.ReadAllText(It.IsAny<string>())).Returns("<html>{{SUMMARY_TOTAL}}{{SUMMARY_PASSED}}{{SUMMARY_FAILED}}{{TEST_RESULTS_ROWS}}</html>");
            
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            
            // Create test runs with different outcomes
            var testRun1 = CreateDummyTestRun("test1", true);
            var testRun2 = CreateDummyTestRun("test2", false);
            
            testRun1.Results.UnitTestResults[0].StartTime = DateTime.Now.AddMinutes(-10);
            testRun1.Results.UnitTestResults[0].EndTime = DateTime.Now.AddMinutes(-5);
            testRun1.Results.UnitTestResults[0].Duration = "00:05:00";
            
            testRun2.Results.UnitTestResults[0].StartTime = DateTime.Now.AddMinutes(-5);
            testRun2.Results.UnitTestResults[0].EndTime = DateTime.Now;
            testRun2.Results.UnitTestResults[0].Duration = "00:05:00";

            // Add app URL info to test results
            testRun1.Results.UnitTestResults[0].Output = new TestOutput
            {
                StdOut = @"{""AppURL"": ""https://make.powerapps.com/environments/Default-tenant/apps""}"
            };

            testRun2.Results.UnitTestResults[0].Output = new TestOutput
            {
                StdOut = @"{""AppURL"": ""https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account""}"
            };

            // Create a list with the test runs
            var testRuns = new List<TestRun> { testRun1, testRun2 };

            // Act
            var result = testRunSummary.GenerateHtmlReport(testRuns);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("2", result); // Total tests
            Assert.Contains("1", result); // Passed tests
            Assert.Contains("1", result); // Failed tests
        }

        [Fact]
        public void TestRunSummary_ImplementsITestRunSummaryInterface()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var testRunSummary = new TestRunSummary(mockFileSystem.Object);

            // Act & Assert
            Assert.IsAssignableFrom<ITestRunSummary>(testRunSummary);
        }

        [Fact]
        public void TestRunSummary_RefactoredMethodsTest()
        {
            // This test verifies that the TestRunSummary class has all the public methods 
            // that were added during refactoring
            
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var testRunSummary = new TestRunSummary(mockFileSystem.Object);
            var type = typeof(TestRunSummary);

            // Act
            var loadTestRunMethod = type.GetMethod("LoadTestRunFromFile");
            var getTemplateContextMethod = type.GetMethod("GetTemplateContext");
            var generateHtmlReportMethod = type.GetMethod("GenerateHtmlReport");
            var getAppTypeAndEntityFromUrlMethod = type.GetMethod("GetAppTypeAndEntityFromUrl");
            
            // Assert
            Assert.NotNull(loadTestRunMethod);
            Assert.Equal("LoadTestRunFromFile", loadTestRunMethod.Name);
            Assert.True(loadTestRunMethod.IsPublic);

            Assert.NotNull(getTemplateContextMethod);
            Assert.Equal("GetTemplateContext", getTemplateContextMethod.Name);
            Assert.True(getTemplateContextMethod.IsPublic);

            Assert.NotNull(generateHtmlReportMethod);
            Assert.Equal("GenerateHtmlReport", generateHtmlReportMethod.Name);
            Assert.True(generateHtmlReportMethod.IsPublic);

            Assert.NotNull(getAppTypeAndEntityFromUrlMethod);
            Assert.Equal("GetAppTypeAndEntityFromUrl", getAppTypeAndEntityFromUrlMethod.Name);
            Assert.True(getAppTypeAndEntityFromUrlMethod.IsPublic);
        }
    }
}
