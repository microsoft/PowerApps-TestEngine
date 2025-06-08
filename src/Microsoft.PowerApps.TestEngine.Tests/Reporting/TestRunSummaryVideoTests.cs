// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestRunSummaryVideoTests
    {
        private Mock<IFileSystem> MockFileSystem;

        public TestRunSummaryVideoTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }

        [Fact]
        public void TestRunSummary_FindsAndIncludesVideoFiles()
        {
            // Arrange
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\Output\summary.html";
            var outputDirectory = Path.GetDirectoryName(outputPath);
            var trxFilePath = @"C:\TestResults\TestResults.trx";
            var trxDirectory = Path.GetDirectoryName(trxFilePath);
            var videoPath = @"C:\TestResults\test_recording.webm";

            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory)).Returns(new string[] { videoPath });
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));
            MockFileSystem.Setup(x => x.GetFileSize(videoPath)).Returns(500 * 1024); // 500KB - valid size
            MockFileSystem.Setup(x => x.Exists(videoPath)).Returns(true);

            // Create test run
            var testRun = new TestRun
            {
                Name = "Test Run with Video",
                Id = "video-test-run",
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
                            TestName = "Test1",
                            Outcome = TestReporter.PassedResultOutcome,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now.AddSeconds(30),
                            Duration = "00:00:30",
                            Output = new TestOutput { StdOut = "Test output" }
                        },
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test2",
                            Outcome = TestReporter.PassedResultOutcome,
                            StartTime = DateTime.Now.AddSeconds(35),
                            EndTime = DateTime.Now.AddSeconds(65),
                            Duration = "00:00:30",
                            Output = new TestOutput { StdOut = "Test output" }
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Outcome = "Completed",
                    Counters = new TestCounters
                    {
                        Total = 2,
                        Executed = 2,
                        Passed = 2,
                        Failed = 0
                    },
                    Output = new TestOutput
                    {
                        StdOut = "{ \"AppURL\": \"https://example.com\", \"TestResults\": \"C:\\\\Results\\\" }"
                    }
                }
            };

            // Serialize the test run
            var serializer = new XmlSerializer(typeof(TestRun));
            string trxContent;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun);
                trxContent = writer.ToString();
            }

            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(trxContent);

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Act
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);

            // Configure the test run summary to return our TRX file
            testRunSummary.GetTrxFiles = (directory) => new[] { trxFilePath };

            // Configure the test run summary to return our video file
            testRunSummary.GetVideoFiles = (directory) => new[] { videoPath };

            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Assert
            Assert.Equal(outputPath, result);
            MockFileSystem.Verify(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true), Times.Once);

            // Verify the HTML content contains our video elements
            Assert.NotNull(capturedHtml);
            Assert.Contains("video-container", capturedHtml);
            Assert.Contains(videoPath.Replace("\\", "//"), capturedHtml);
            Assert.Contains("Play Video", capturedHtml);
            Assert.Contains("video-controls", capturedHtml);
            Assert.Contains("Copy Timecode", capturedHtml);

            // Verify timecode functionality
            Assert.Contains("data-timecode=", capturedHtml);
            Assert.Contains("video-timestamp", capturedHtml);
            Assert.Contains("00:00:00", capturedHtml);
        }

        [Fact]
        public void TestRunSummary_HandlesMultipleVideoFiles()
        {
            // Arrange
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\Output\summary.html";
            var outputDirectory = Path.GetDirectoryName(outputPath);
            var trxFilePath = @"C:\TestResults\TestResults.trx";
            var trxDirectory = Path.GetDirectoryName(trxFilePath);
            var videoPath1 = @"C:\TestResults\test_recording1.webm";
            var videoPath2 = @"C:\TestResults\test_recording2.webm";
            var smallVideoPath = @"C:\TestResults\incomplete_recording.webm";
            // Setup mock file system
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(outputDirectory)).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(outputDirectory));
            MockFileSystem.Setup(x => x.GetFiles(resultsDirectory)).Returns(new string[] { videoPath1, videoPath2, smallVideoPath });
            MockFileSystem.Setup(x => x.GetFileSize(videoPath1)).Returns(500 * 1024); // 500KB - valid size
            MockFileSystem.Setup(x => x.GetFileSize(videoPath2)).Returns(200 * 1024); // 200KB - valid size
            MockFileSystem.Setup(x => x.GetFileSize(smallVideoPath)).Returns(5 * 1024); // 5KB - too small, should be filtered out
            MockFileSystem.Setup(x => x.Exists(videoPath1)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(videoPath2)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(smallVideoPath)).Returns(true);

            // Create test run
            var testRun = new TestRun
            {
                Name = "Test Run with Multiple Videos",
                Id = "multiple-videos-test-run",
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test1",
                            Outcome = TestReporter.PassedResultOutcome,
                            Duration = "00:00:30"
                        },
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test2",
                            Outcome = TestReporter.PassedResultOutcome,
                            Duration = "00:00:45"
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput
                    {
                        StdOut = "{ \"TestResults\": \"C:\\\\Results\" }"
                    }
                }
            };

            // Serialize the test run
            var serializer = new XmlSerializer(typeof(TestRun));
            string trxContent;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun);
                trxContent = writer.ToString();
            }

            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(trxContent);

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Act
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);

            // Configure the test run summary to return our TRX file
            testRunSummary.GetTrxFiles = (directory) => new[] { trxFilePath };

            // Configure the test run summary to return our video files including the small one
            testRunSummary.GetVideoFiles = (directory) => new[] { videoPath1, videoPath2, smallVideoPath };

            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Assert
            Assert.Equal(outputPath, result);

            // Verify the HTML content contains our valid video elements but not the small one
            Assert.NotNull(capturedHtml);
            Assert.Contains(videoPath1.Replace("\\", "\\\\"), capturedHtml);
            Assert.Contains(videoPath2.Replace("\\", "\\\\"), capturedHtml);
            Assert.DoesNotContain(smallVideoPath.Replace("\\", "\\\\"), capturedHtml);

            // Verify it contains the "Videos" array for multiple videos
            Assert.Contains("&quot;videos&quot;:", capturedHtml);
        }

        [Fact]
        public void TestRunSummary_CalculatesTimecodes()
        {
            // Arrange
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\Output\summary.html";
            var trxFilePath = @"C:\TestResults\TestResults.trx";
            var videoPath = @"C:\TestResults\test_recording.webm";

            // Setup basic mocks
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(Path.GetDirectoryName(outputPath))).Returns(true);
            MockFileSystem.Setup(x => x.GetFileSize(videoPath)).Returns(500 * 1024);
            MockFileSystem.Setup(x => x.Exists(videoPath)).Returns(true);

            // Create test run with specific durations for deterministic testing
            var testRun = new TestRun
            {
                Name = "Timecode Test Run",
                Id = "timecode-test",
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = "test-1",
                            TestName = "First Test - 30s",
                            Outcome = TestReporter.PassedResultOutcome,
                            Duration = "00:00:30",
                            Output = new TestOutput()
                        },
                        new UnitTestResult
                        {
                            TestId = "test-2",
                            TestName = "Second Test - 45s",
                            Outcome = TestReporter.PassedResultOutcome,
                            Duration = "00:00:45",
                            Output = new TestOutput()
                        },
                        new UnitTestResult
                        {
                            TestId = "test-3",
                            TestName = "Third Test - 60s",
                            Outcome = TestReporter.PassedResultOutcome,
                            Duration = "00:01:00",
                            Output = new TestOutput()
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput
                    {
                        StdOut = "{ }"
                    }
                }
            };

            // Serialize the test run
            var serializer = new XmlSerializer(typeof(TestRun));
            string trxContent;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun);
                trxContent = writer.ToString();
            }

            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(trxContent);

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Act
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) => new[] { trxFilePath };
            testRunSummary.GetVideoFiles = (directory) => new[] { videoPath };

            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Assert
            Assert.Equal(outputPath, result);
            Assert.NotNull(capturedHtml);

            // Verify the video controls include these timecodes
            Assert.Contains("data-timecode=\"00:00:00\"", capturedHtml);
            Assert.Contains("data-timecode=\"00:00:30\"", capturedHtml);
            Assert.Contains("data-timecode=\"00:01:00\"", capturedHtml);
        }

        [Fact]
        public void TestRunSummary_HandlesNoVideos()
        {
            // Arrange
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\Output\summary.html";
            var trxFilePath = @"C:\TestResults\TestResults.trx";

            // Setup basic mocks
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockFileSystem.Setup(x => x.Exists(Path.GetDirectoryName(outputPath))).Returns(true);

            // Create a basic test run
            var testRun = new TestRun
            {
                Name = "No Video Test Run",
                Id = "no-video-test",
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = "test-1",
                            TestName = "Simple Test",
                            Outcome = TestReporter.PassedResultOutcome,
                            Duration = "00:00:30"
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput
                    {
                        StdOut = "{ }"
                    }
                }
            };

            // Serialize the test run
            var serializer = new XmlSerializer(typeof(TestRun));
            string trxContent;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, testRun);
                trxContent = writer.ToString();
            }

            MockFileSystem.Setup(x => x.ReadAllText(trxFilePath)).Returns(trxContent);

            string capturedHtml = null;
            MockFileSystem.Setup(x => x.WriteTextToFile(outputPath, It.IsAny<string>(), true))
                .Callback<string, string, bool>((path, content, append) => capturedHtml = content);

            // Act
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            testRunSummary.GetTrxFiles = (directory) => new[] { trxFilePath };
            testRunSummary.GetVideoFiles = (directory) => Array.Empty<string>(); // Return no video files

            var result = testRunSummary.GenerateSummaryReport(resultsDirectory, outputPath);

            // Assert
            Assert.Equal(outputPath, result);
            Assert.NotNull(capturedHtml);

            // Report should still be generated without video-related elements
            Assert.DoesNotContain("videos&quot;", capturedHtml); // No video container elements
        }
    }
}
