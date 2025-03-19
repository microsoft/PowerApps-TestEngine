// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;

namespace testengine.module
{
    public class ConsentDialogFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IPage> MockPage;
        private PowerFxConfig TestConfig;
        private NetworkRequestMock TestNetworkRequestMock;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<IFrame> MockFrame;

        public ConsentDialogFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestWebProvider = new Mock<ITestWebProvider>();
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            TestConfig = new PowerFxConfig();
            TestNetworkRequestMock = new NetworkRequestMock();
            MockLogger = new Mock<ILogger>();
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockFrame = new Mock<IFrame>(MockBehavior.Strict);
        }

        [Fact]
        public async Task ExitIfConsentFound()
        {
            // Arrange
            var function = new ConsentDialogFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            MockTestState.Setup(x => x.GetTimeout()).Returns(0);
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.Setup(x => x.Url).Returns("https://contoso.crm.dynamics.com/main.aspx");
            MockPage.Setup(x => x.Frames).Returns(new List<IFrame>() { MockFrame.Object });
            MockFrame.Setup(x => x.Url).Returns("https://login.microsoft.com/consent");

            var MockLocator = new Mock<ILocator>(MockBehavior.Strict);

            FrameGetByRoleOptions optionValues = null;
            MockFrame.Setup(x => x.GetByRole(AriaRole.Button, It.IsAny<FrameGetByRoleOptions>()))
                .Callback((AriaRole role, FrameGetByRoleOptions options) => optionValues = options)
                .Returns(MockLocator.Object);

            MockLocator.Setup(x => x.IsEnabledAsync(null)).Returns(Task.FromResult(true));
            MockLocator.Setup(x => x.ClickAsync(null)).Returns(Task.CompletedTask);

            var recordType = RecordType.Empty()
               .Add(new NamedFormulaType("Text", FormulaType.String, displayName: "Text"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("Text", FormulaValue.New("Text"))
            );

            var table = TableValue.NewTable(recordType, rv1);

            // Act
            function.Execute(table);

            // Assert
            Assert.Equal("Allow", optionValues.Name);
            Assert.True(optionValues.Exact);
        }

        [Fact]
        public async Task ExitPageTextMatch()
        {
            // Arrange
            var function = new ConsentDialogFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            MockTestState.Setup(x => x.GetTimeout()).Returns(0);
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.Setup(x => x.Url).Returns("https://contoso.crm.dynamics.com/main.aspx");
            MockPage.Setup(x => x.Frames).Returns(new List<IFrame>() { });


            var MockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.GetByText("Text", null)).Returns(MockLocator.Object);
            MockLocator.Setup(x => x.CountAsync()).Returns(Task.FromResult(1));

            var recordType = RecordType.Empty()
               .Add(new NamedFormulaType("Text", FormulaType.String, displayName: "Text"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("Text", FormulaValue.New("Text"))
            );

            var table = TableValue.NewTable(recordType, rv1);

            // Act
            function.Execute(table);

            // Assert

        }

        [Fact]
        public async Task ThrowsExceptionIfTimeout()
        {
            // Arrange
            var function = new ConsentDialogFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            MockTestState.Setup(x => x.GetTimeout()).Returns(0);
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.Setup(x => x.Url).Returns("https://contoso.crm.dynamics.com/main.aspx");
            MockPage.Setup(x => x.Frames).Returns(new List<IFrame>() { });


            var MockLocator = new Mock<ILocator>(MockBehavior.Strict);
            MockPage.Setup(x => x.GetByText("Other Value", null)).Returns(MockLocator.Object);
            MockLocator.Setup(x => x.CountAsync()).Returns(Task.FromResult(0));

            var recordType = RecordType.Empty()
               .Add(new NamedFormulaType("Text", FormulaType.String, displayName: "Text"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("Text", FormulaValue.New("Other Value"))
            );

            var table = TableValue.NewTable(recordType, rv1);

            // Act
            Assert.Throws<AggregateException>(() => function.Execute(table));

            // Assert

        }
    }
}
