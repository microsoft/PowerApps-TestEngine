// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class PowerAppsUrlMapperTests
    {
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;

        public PowerAppsUrlMapperTests()
        {
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Theory]
        [InlineData("myEnvironment", "Prod", "myApp", "myTenant", "https://apps.powerapps.com/play/e/myEnvironment/an/myApp?tenantId=myTenant&source=testengine&enablePATest=true&patestSDKVersion=0.0.1", "&enablePATest=true&patestSDKVersion=0.0.1")]
        [InlineData("myEnvironment", "Prod", "myApp", "myTenant", "https://apps.powerapps.com/play/e/myEnvironment/an/myApp?tenantId=myTenant&source=testengine", "")]
        [InlineData("defaultEnvironment", "Test", "defaultApp", "defaultTenant", "https://apps.test.powerapps.com/play/e/defaultEnvironment/an/defaultApp?tenantId=defaultTenant&source=testengine", "")]
        [InlineData("defaultEnvironment", "test", "defaultApp", "defaultTenant", "https://apps.test.powerapps.com/play/e/defaultEnvironment/an/defaultApp?tenantId=defaultTenant&source=testengine", "")]
        [InlineData("myEnvironment", "PROD", "myApp", "myTenant", "https://apps.powerapps.com/play/e/myEnvironment/an/myApp?tenantId=myTenant&source=testengine", "")]
        [InlineData("myEnvironment", "prod", "myApp", "myTenant", "https://apps.powerapps.com/play/e/myEnvironment/an/myApp?tenantId=myTenant&source=testengine", "")]
        [InlineData("defaultEnvironment", "", "defaultApp", "defaultTenant", "https://apps.powerapps.com/play/e/defaultEnvironment/an/defaultApp?tenantId=defaultTenant&source=testengine", "")]
        [InlineData("defaultEnvironment", null, "defaultApp", "defaultTenant", "https://apps.powerapps.com/play/e/defaultEnvironment/an/defaultApp?tenantId=defaultTenant&source=testengine", "")]
        public void GenerateAppUrlTest(string environmentId, string cloud, string appLogicalName, string tenantId, string expectedAppUrl, string additionalQuery)
        {
            MockTestState.Setup(x => x.GetEnvironment()).Returns(environmentId);
            MockTestState.Setup(x => x.GetCloud()).Returns(cloud);
            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(new TestSuiteDefinition() { AppLogicalName = appLogicalName });
            MockTestState.Setup(x => x.GetTenant()).Returns(tenantId);
            var powerAppUrlMapper = new PowerAppsUrlMapper(MockTestState.Object, MockSingleTestInstanceState.Object);
            Assert.Equal(expectedAppUrl, powerAppUrlMapper.GenerateTestUrl(additionalQuery));
            MockTestState.Verify(x => x.GetEnvironment(), Times.Once());
            MockTestState.Verify(x => x.GetCloud(), Times.Once());
            MockSingleTestInstanceState.Verify(x => x.GetTestSuiteDefinition(), Times.Once());
            MockTestState.Verify(x => x.GetTenant(), Times.Once());
        }

        [Theory]
        [InlineData("", "appLogicalName", "tenantId")]
        [InlineData(null, "appLogicalName", "tenantId")]
        [InlineData("environmentId", "", "tenantId")]
        [InlineData("environmentId", null, "tenantId")]
        [InlineData("environmentId", "appLogicalName", "")]
        [InlineData("environmentId", "appLogicalName", null)]
        public void GenerateLoginUrlThrowsOnInvalidSetupTest(string environmentId, string appLogicalName, string tenantId)
        {
            MockTestState.Setup(x => x.GetEnvironment()).Returns(environmentId);
            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(new TestSuiteDefinition() { AppLogicalName = appLogicalName });
            MockTestState.Setup(x => x.GetTenant()).Returns(tenantId);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppUrlMapper = new PowerAppsUrlMapper(MockTestState.Object, MockSingleTestInstanceState.Object);
            Assert.Throws<InvalidOperationException>(() => powerAppUrlMapper.GenerateTestUrl(""));
        }

        [Fact]
        public void GenerateLoginUrlThrowsOnInvalidTestDefinitionTest()
        {
            TestSuiteDefinition testDefinition = null;
            MockTestState.Setup(x => x.GetEnvironment()).Returns("environmentId");
            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testDefinition);
            MockTestState.Setup(x => x.GetTenant()).Returns("tenantId");
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            var powerAppUrlMapper = new PowerAppsUrlMapper(MockTestState.Object, MockSingleTestInstanceState.Object);
            Assert.Throws<InvalidOperationException>(() => powerAppUrlMapper.GenerateTestUrl(""));
        }
    }
}
