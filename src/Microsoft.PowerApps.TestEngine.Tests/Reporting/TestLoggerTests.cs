// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
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
            var testLogger = new TestLogger(MockFileSystem.Object, LogLevel.Debug);
            Assert.Equal(shouldBeEnabled, testLogger.IsEnabled(level));
        }

        [Fact]
        public void WriteToLogsFileThrowsOnInvalidPathTest()
        {
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            var testLogger = new TestLogger(MockFileSystem.Object, LogLevel.Debug);
            Assert.Throws<ArgumentException>(() => testLogger.WriteToLogsFile(""));
            MockFileSystem.Verify(x => x.IsValidFilePath(""), Times.Once());
        }

        [Fact]
        public void WriteToLogsFileTest()
        {
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            MockFileSystem.Setup(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string[]>()));
            var testLogger = new TestLogger(MockFileSystem.Object, LogLevel.Debug);

            var debugLogs = new List<string>();
            var logs = new List<string>();

            for (var i = 0; i < 10; i++)
            {
                debugLogs.Add($"Logging: {i}");
            }

            for (var i = 0; i < 10; i++)
            {
                debugLogs.Add($"Logging: {i + 10}");
                logs.Add($"Logging: {i + 10}");
            }

            testLogger.DebugLogs = debugLogs;
            testLogger.Logs = logs;

            var directoryPath = "C:\\Logs";
            testLogger.WriteToLogsFile(directoryPath);
            MockFileSystem.Verify(x => x.IsValidFilePath(directoryPath), Times.Once());
            MockFileSystem.Verify(x => x.WriteTextToFile(Path.Combine(directoryPath, "debugLogs.txt"), debugLogs.ToArray()), Times.Once());
            MockFileSystem.Verify(x => x.WriteTextToFile(Path.Combine(directoryPath, "logs.txt"), logs.ToArray()), Times.Once());
        }



        [Theory]
        [InlineData(LogLevel.Warning, true, true)]
        [InlineData(LogLevel.Critical, true, true)]
        [InlineData(LogLevel.Error, true, true)]
        public void LogTestLevel(LogLevel level, bool shouldBeInDebugLogs, bool shouldBeInLogs)
        {
            var testLogger = new TestLogger(MockFileSystem.Object, LogLevel.Trace);

            var expectedMessages = new List<string>();

            for (var i = 0; i < 5; i++)
            {
                var id = Guid.NewGuid();
                var stringFormat = "Id: {0}";
                expectedMessages.Add($"[{level}]: {String.Format(stringFormat, id)}{Environment.NewLine}");
                testLogger.Log(level, stringFormat, id);
            }

            foreach (var message in expectedMessages)
            {
                Assert.Equal(shouldBeInDebugLogs, testLogger.DebugLogs.Contains(message));
                Assert.Equal(shouldBeInLogs, testLogger.Logs.Contains(message));
            }
        }



        [Theory]
        [InlineData(LogLevel.Trace, true, false)]
        [InlineData(LogLevel.Debug, true, false)]
        [InlineData(LogLevel.Information, true, true)]
        [InlineData(LogLevel.None, true, true)]
        public void LogTest(LogLevel level, bool shouldBeInDebugLogs, bool shouldBeInLogs)
        {
            var testLogger = new TestLogger(MockFileSystem.Object, LogLevel.Trace);

            var expectedMessages = new List<string>();

            for (var i = 0; i < 5; i++)
            {
                var id = Guid.NewGuid();
                var stringFormat = "Id: {0}";
                expectedMessages.Add($"{String.Format(stringFormat, id)}{Environment.NewLine}");
                testLogger.Log(level, stringFormat, id);
            }

            foreach (var message in expectedMessages)
            {
                Assert.Equal(shouldBeInDebugLogs, testLogger.DebugLogs.Contains(message));
                Assert.Equal(shouldBeInLogs, testLogger.Logs.Contains(message));
            }
        }

    }
}
