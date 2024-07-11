// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppPortalFunctionsTest
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerAppPortalFunctionsTest()
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
            var provider = new PowerAppPortalFunctions();

            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("powerapps.portal", name);
        }

        [Theory]
        [InlineData("", "11112222-3333-4444-5555-66667777888", "https://make.powerapps.com/environments/11112222-3333-4444-5555-66667777888", "?source=testengine")]
        [InlineData("gcc", "11112222-3333-4444-5555-66667777888", "https://make.gov.powerapps.us/environments/11112222-3333-4444-5555-66667777888", "?source=testengine")]
        [InlineData("gcchigh", "11112222-3333-4444-5555-66667777888", "https://make.high.powerapps.us/environments/11112222-3333-4444-5555-66667777888", "?source=testengine")]
        [InlineData("dod", "11112222-3333-4444-5555-66667777888", "https://make.apps.appsplatform.us/environments/11112222-3333-4444-5555-66667777888", "?source=testengine")]
        public void GenerateExpectedTestUrlForDomainAndEnvironment(string domain, string environmentId, string expectedBaseUrl, string expectedParameters)
        {
            // Arrange
            var provider = new PowerAppPortalFunctions(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);

            MockTestState.Setup(x => x.GetEnvironment()).Returns(environmentId);
            MockTestState.Setup(x => x.SetDomain(expectedBaseUrl));

            // Act
            var url = provider.GenerateTestUrl(domain, String.Empty);

            // Assert
            Assert.Equal(expectedBaseUrl + expectedParameters, url);
        }
    }
}
