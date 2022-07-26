// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestLoggerTests
    {
        private Mock<IFileSystem> MockFileSystem;

        public TestLoggerTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }

        [Fact]
        public void BeginScopeTest()
        {
            var testLogger = new TestLogger(MockFileSystem.Object);
            Assert.Null(testLogger.BeginScope("hello"));
            Assert.Null(testLogger.BeginScope(new Dictionary<string, string>()));
        }

        [Theory]
        [InlineData(LogLevel.Critical, true)]
        [InlineData(LogLevel.Debug, true)]
        [InlineData(LogLevel.Error, true)]
        [InlineData(LogLevel.Information, true)]
        [InlineData(LogLevel.None, true)]
        [InlineData(LogLevel.Trace, true)]
        [InlineData(LogLevel.Warning, true)]
        public void IsEnabledTest(LogLevel level, bool shouldBeEnabled)
        {
            var testLogger = new TestLogger(MockFileSystem.Object);
            Assert.Equal(shouldBeEnabled, testLogger.IsEnabled(level));
        }

        [Fact]
        public void WriteToLogsFileThrowsOnInvalidPathTest()
        {
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            var testLogger = new TestLogger(MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => testLogger.WriteToLogsFile(""));
            MockFileSystem.Verify(x => x.IsValidFilePath(""), Times.Once());
        }

        [Fact]
        public void WriteToLogsFileTest()
        {
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            MockFileSystem.Setup(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string[]>()));
            var testLogger = new TestLogger(MockFileSystem.Object);

            var logs = new List<string>();

            for (var i = 0; i < 10; i++)
            {
                logs.Add($"Logging: {i + 10}");
            }

            testLogger.Logs = logs;

            var directoryPath = "C:\\Logs";
            testLogger.WriteToLogsFile(directoryPath);
            MockFileSystem.Verify(x => x.IsValidFilePath(directoryPath), Times.Once());
            MockFileSystem.Verify(x => x.WriteTextToFile(Path.Combine(directoryPath, "logs.txt"), logs.ToArray()), Times.Once());
        }



        [Theory]
        [InlineData(LogLevel.Critical, true, true)]
        [InlineData(LogLevel.Debug, true, false)]
        [InlineData(LogLevel.Error, true, true)]
        [InlineData(LogLevel.Information, true, true)]
        [InlineData(LogLevel.None, true, true)]
        [InlineData(LogLevel.Trace, true, false)]
        [InlineData(LogLevel.Warning, true, true)]
        public void LogTest(LogLevel level, bool shouldBeInDebugLogs, bool shouldBeInLogs)
        {
            var testLogger = new TestLogger(MockFileSystem.Object);

            var expectedMessages = new List<string>();

            for(var i =0; i < 5; i++)
            {
                var id = Guid.NewGuid();
                var stringFormat = "Id: {0}";
                expectedMessages.Add($"[{level}] - [0]: {String.Format(stringFormat, id)}{Environment.NewLine}");
                testLogger.Log(level, stringFormat, id);
            }

            foreach(var message in expectedMessages)
            {
                Assert.Equal(shouldBeInLogs, testLogger.Logs.Contains(message));
            }
        }
    }
}
