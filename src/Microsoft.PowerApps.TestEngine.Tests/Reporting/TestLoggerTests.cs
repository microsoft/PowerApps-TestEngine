// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [Fact]
        public void BeginScopeThrowsOnInvalidInputTest()
        {
            var testLogger = new TestLogger(MockFileSystem.Object);
            Assert.Throws<InvalidOperationException>(() => testLogger.BeginScope(""));
            Assert.Throws<InvalidOperationException>(() => testLogger.BeginScope(null));
            Assert.Throws<InvalidOperationException>(() => testLogger.BeginScope(new Dictionary<string, string>()));
            Assert.Throws<InvalidOperationException>(() => testLogger.BeginScope(1));
        }

        [Fact]
        public void BeginScopeThrowsIfScopeAlreadyBegunTest()
        {
            var testLogger = new TestLogger(MockFileSystem.Object);
            Assert.NotNull(testLogger.BeginScope(Guid.NewGuid().ToString()));
            Assert.Throws<InvalidOperationException>(() => testLogger.BeginScope(Guid.NewGuid().ToString()));
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
            var testLogger = new TestLogger(MockFileSystem.Object);
            var createdLogs = new Dictionary<string, string[]>();

            MockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            MockFileSystem.Setup(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string[]>())).Callback((string filePath, string[] logs) =>
            {
                createdLogs.Add(filePath, logs);
            });

            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            testLogger.WriteToLogsFile("", "");
            MockFileSystem.Verify(x => x.Exists(""), Times.Once());
        }

        [Fact]
        public void LoggerTest()
        {
            var scopeId1 = Guid.NewGuid().ToString();
            var scopeId2 = Guid.NewGuid().ToString();
            var createdLogs = new Dictionary<string, string[]>();

            MockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
            MockFileSystem.Setup(x => x.WriteTextToFile(It.IsAny<string>(), It.IsAny<string[]>())).Callback((string filePath, string[] logs) =>
            {
                createdLogs.Add(filePath, logs);
            });

            var testLogger = new TestLogger(MockFileSystem.Object);

            // Test data is in the format:
            // expectedMessage, shouldBeInLogs, shouldBeInFirstScopeLogs, shouldBeInSecondScopeLogs
            var expectedResults = new List<(string, bool, bool, bool)>();

            var logLevels = new LogLevel[] { LogLevel.Critical, LogLevel.Error, LogLevel.Warning, LogLevel.Information, LogLevel.None };
            var debugLogLevels = new LogLevel[] { LogLevel.Debug, LogLevel.Trace };

            var logGenerator = (string tag, bool shouldBeInFirstScopeLogs, bool shouldBeInSecondScopeLogs) =>
            {
                foreach (var level in logLevels)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        var id = Guid.NewGuid();
                        var stringFormat = "LogLevel: {0} Tag: {1} Id: {2}";
                        expectedResults.Add((String.Format(stringFormat, level, tag, id), true, shouldBeInFirstScopeLogs, shouldBeInSecondScopeLogs));
                        testLogger.Log(level, stringFormat, level, tag, id);

                        if (i == 3)
                        {
                            // throw in some debug logs in the middle
                            foreach (var debugLevel in debugLogLevels)
                            {
                                var debugId = Guid.NewGuid();
                                expectedResults.Add((String.Format(stringFormat, debugLevel, tag, debugId), false, shouldBeInFirstScopeLogs, shouldBeInSecondScopeLogs));
                                testLogger.Log(debugLevel, stringFormat, debugLevel, tag, debugId);
                            }
                        }
                    }
                }
            };

            logGenerator("Before Scope 1", false, false);

            using (var scope1 = testLogger.BeginScope(scopeId1))
            {
                logGenerator("Scope 1", true, false);
            }

            logGenerator("After Scope 1/Before Scope 2", false, false);

            using (var scope2 = testLogger.BeginScope(scopeId2))
            {
                logGenerator("Scope 2", false, true);
            }

            logGenerator("After Scope 2", false, false);


            var writeLogsToFileAndValidate = (string filter, string directoryPath, string[] expectedDebugLogs, string[] expectedLogs) =>
            {
                testLogger.WriteToLogsFile(directoryPath, filter);
                //assuming input directory names don't exist on the machine and have to be created
                MockFileSystem.Verify(x => x.Exists(directoryPath), Times.Once());
                MockFileSystem.Verify(x => x.CreateDirectory(directoryPath), Times.Never());
                var debugLogPath = Path.Combine(directoryPath, "debugLogs.txt");
                var logPath = Path.Combine(directoryPath, "logs.txt");
                Assert.True(createdLogs.ContainsKey(debugLogPath));
                Assert.True(createdLogs.ContainsKey(logPath));
                var debugLogs = createdLogs[debugLogPath];
                var logs = createdLogs[logPath];

                Assert.Equal(expectedDebugLogs.Length, debugLogs.Length);
                for (var i = 0; i < expectedDebugLogs.Length; i++)
                {
                    Assert.True(debugLogs[i].IndexOf(expectedDebugLogs[i]) >= 0);
                }

                Assert.Equal(expectedLogs.Length, logs.Length);
                for (var i = 0; i < expectedLogs.Length; i++)
                {
                    Assert.True(logs[i].IndexOf(expectedLogs[i]) >= 0);
                }
            };

            // Get all logs
            var allLogsDirectoryPath = "C:\\AllLogs";
            var allLogsExpectedDebugLogs = expectedResults.Select(x => x.Item1).ToArray();
            var allLogsExpectedLogs = expectedResults.Where(x => x.Item2).Select(x => x.Item1).ToArray();
            writeLogsToFileAndValidate("", allLogsDirectoryPath, allLogsExpectedDebugLogs, allLogsExpectedLogs);

            // Get scope 1 logs
            var scope1LogsDirectoryPath = "C:\\Scope1Logs";
            var scope1ExpectedDebugLogs = expectedResults.Where(x => x.Item3).Select(x => x.Item1).ToArray();
            var scope1ExpectedLogs = expectedResults.Where(x => x.Item2 && x.Item3).Select(x => x.Item1).ToArray();
            writeLogsToFileAndValidate(scopeId1, scope1LogsDirectoryPath, scope1ExpectedDebugLogs, scope1ExpectedLogs);

            // Get scope 2 logs
            var scope2LogsDirectoryPath = "C:\\Scope2Logs";
            var scope2ExpectedDebugLogs = expectedResults.Where(x => x.Item4).Select(x => x.Item1).ToArray();
            var scope2ExpectedLogs = expectedResults.Where(x => x.Item2 && x.Item4).Select(x => x.Item1).ToArray();
            writeLogsToFileAndValidate(scopeId2, scope2LogsDirectoryPath, scope2ExpectedDebugLogs, scope2ExpectedLogs);
        }
    }
}
