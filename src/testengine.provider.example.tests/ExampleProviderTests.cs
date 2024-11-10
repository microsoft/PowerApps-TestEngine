// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class ExampleProviderTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public ExampleProviderTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void ExpectedName()
        {
            // Arrange
            var provider = new ExampleProvider();

            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("example", name);
        }

        [Theory]
        [InlineData("", "about:blank")]
        [InlineData("gcc", "about:blank")]
        [InlineData("gcchigh", "about:blank")]
        [InlineData("dod", "about:blank")]
        public void GenerateExpectedTestUrlForDomain(string domain, string expectedUrl)
        {
            // Arrange
            var provider = new ExampleProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            MockTestState.Setup(m => m.SetDomain(expectedUrl));

            // Act
            var url = provider.GenerateTestUrl(domain, String.Empty);

            // Assert
            Assert.Equal(expectedUrl, url);
        }
    }
}
