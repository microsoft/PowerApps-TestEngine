// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.CopilotPortal.Tests
{
    public class CopilotPortalProviderTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public CopilotPortalProviderTest()
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
            var provider = new CopilotPortalProvider();

            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("copilot.portal", name);
        }

        [Theory]
        [InlineData("", "11112222-3333-4444-5555-66667777888", "https://copilotstudio.microsoft.com/environments/11112222-3333-4444-5555-66667777888/bots/TEST/overview")]
        [InlineData("gcc", "11112222-3333-4444-5555-66667777888", "https://gcc.powerva.microsoft.us/environments/11112222-3333-4444-5555-66667777888/bots/TEST/overview")]
        [InlineData("gcchigh", "11112222-3333-4444-5555-66667777888", "http://high.powerva.microsoft.us/environments/11112222-3333-4444-5555-66667777888/bots/TEST/overview")]
        public void GenerateExpectedTestUrlForDomainAndEnvironment(string domain, string environmentId, string expectedBaseUrl)
        {
            // Arrange
            var provider = new CopilotPortalProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            MockTestState.Setup(x => x.GetEnvironment()).Returns(environmentId);
            MockTestState.Setup(x => x.SetDomain(expectedBaseUrl));
            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(new TestSuiteDefinition { AppId = "TEST" });

            // Act
            var url = provider.GenerateTestUrl(domain, String.Empty);

            // Assert
            Assert.Equal(expectedBaseUrl, url);
        }
    }
}
