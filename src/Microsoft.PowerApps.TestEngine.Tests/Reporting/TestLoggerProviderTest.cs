// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestLoggerProviderTest
    {
        public void CreateLoggerTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var testLoggerProvider = new TestLoggerProvider(mockFileSystem.Object);

            var category = Guid.NewGuid().ToString();
            var logger = testLoggerProvider.CreateLogger(category);
            Assert.True(TestLoggerProvider.TestLoggers.ContainsKey(category));

            var sameLogger = testLoggerProvider.CreateLogger(category);
            Assert.Equal(logger, sameLogger);

            var category2 = Guid.NewGuid().ToString();
            var logger2 = testLoggerProvider.CreateLogger(category2);
            Assert.True(TestLoggerProvider.TestLoggers.ContainsKey(category2));
            Assert.NotEqual(logger, logger2);
        }
    }
}
