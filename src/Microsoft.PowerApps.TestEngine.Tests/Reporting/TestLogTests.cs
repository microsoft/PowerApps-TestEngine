// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerApps.TestEngine.Reporting;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    public class TestLogTests
    {
        [Fact]
        public void DateTest()
        {
            // Arrange
            var test = new DateTime(2022, 11, 16);
            var log = new TestLog() { TimeStamper = () => test };

            // Act & Assert
            Assert.Equal(test, log.When);

            // Assert
        }
    }
}
