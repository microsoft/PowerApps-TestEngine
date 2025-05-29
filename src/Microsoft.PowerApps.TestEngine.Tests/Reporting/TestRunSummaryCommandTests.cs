// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestRunSummaryCommandTests
    {
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ITestRunSummary> MockTestRunSummary;

        public TestRunSummaryCommandTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockTestRunSummary = new Mock<ITestRunSummary>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("")]
        public void ThrowsOnInvalidResultsDirectoryTest(string resultsDirectory)
        {
            var command = new TestRunSummaryCommand(MockTestRunSummary.Object, MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => command.GenerateSummaryReport(resultsDirectory));
        }
        
        [Fact]
        public void ThrowsOnNullResultsDirectoryTest()
        {
            var command = new TestRunSummaryCommand(MockTestRunSummary.Object, MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => command.GenerateSummaryReport(null));
        }

        [Fact]
        public void ThrowsOnNonExistentResultsDirectoryTest()
        {
            var resultsDirectory = @"C:\NonExistentDirectory";
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(false);
            
            var command = new TestRunSummaryCommand(MockTestRunSummary.Object, MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => command.GenerateSummaryReport(resultsDirectory));
        }

        [Fact]
        public void GenerateSummaryReportWithDefaultOutputPathTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var generatedOutputPath = @"C:\TestResults\TestRunSummary_20250528_235959.html";
            
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockTestRunSummary.Setup(x => x.GenerateSummaryReport(resultsDirectory, It.IsAny<string>())).Returns(generatedOutputPath);
            
            var command = new TestRunSummaryCommand(MockTestRunSummary.Object, MockFileSystem.Object);
            var result = command.GenerateSummaryReport(resultsDirectory);
            
            Assert.Equal(generatedOutputPath, result);
            MockTestRunSummary.Verify(x => x.GenerateSummaryReport(resultsDirectory, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void GenerateSummaryReportWithSpecifiedOutputPathTest()
        {
            var resultsDirectory = @"C:\TestResults";
            var outputPath = @"C:\Output\summary.html";
            
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockTestRunSummary.Setup(x => x.GenerateSummaryReport(resultsDirectory, outputPath)).Returns(outputPath);
            
            var command = new TestRunSummaryCommand(MockTestRunSummary.Object, MockFileSystem.Object);
            var result = command.GenerateSummaryReport(resultsDirectory, outputPath);
            
            Assert.Equal(outputPath, result);
            MockTestRunSummary.Verify(x => x.GenerateSummaryReport(resultsDirectory, outputPath), Times.Once);
        }

        [Fact]
        public void GeneratesSummaryWithTimestampInFilename()
        {
            var resultsDirectory = @"C:\TestResults";
            var timestampPattern = @"TestRunSummary_\d{8}_\d{6}\.html";
            
            MockFileSystem.Setup(x => x.Exists(resultsDirectory)).Returns(true);
            MockTestRunSummary.Setup(x => x.GenerateSummaryReport(resultsDirectory, It.IsRegex(timestampPattern))).Returns(@"C:\TestResults\TestRunSummary_20250528_123456.html");
            
            var command = new TestRunSummaryCommand(MockTestRunSummary.Object, MockFileSystem.Object);
            var result = command.GenerateSummaryReport(resultsDirectory);
            
            Assert.Matches(timestampPattern, Path.GetFileName(result));
            MockTestRunSummary.Verify(x => x.GenerateSummaryReport(resultsDirectory, It.IsRegex(timestampPattern)), Times.Once);
        }
    }
}
