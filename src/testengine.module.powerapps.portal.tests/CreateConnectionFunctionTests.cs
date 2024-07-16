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
using testengine.module.powerapps.portal;

namespace testengine.module.powerappsportal.tests
{
    public class CreateConnectionFunctionTests
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

        public CreateConnectionFunctionTests()
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
        }

        [Theory]
        [InlineData("test", false, "", "")]
        [InlineData("test", true, "", "")]
        [InlineData("test", true, "name", "value")]
        public void ExecuteCreateConnections(string name, bool interactive, string parameterName, string parameterValue)
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.Setup(x => x.NewPageAsync()).Returns(Task.FromResult(MockPage.Object));
            MockTestState.Setup(x => x.GetTimeout()).Returns(1000);
            MockTestState.Setup(x => x.GetDomain()).Returns("https://make.powerapps.com");
            MockPage.Setup(x => x.GotoAsync($"https://make.powerapps.com/connections/available?apiName=test&source=testengine", null)).Returns(Task.FromResult(new Mock<IResponse>().Object));

            var mocKLocator = new Mock<ILocator>();
            mocKLocator.Setup(x => x.IsEnabledAsync(null)).Returns(Task.FromResult(true));
            MockPage.Setup(x => x.Locator(".btn.btn-primary.add:has-text(\"Create\")", null)).Returns(mocKLocator.Object);
            MockPage.Setup(x => x.IsVisibleAsync(".pa-model", null)).Returns(Task.FromResult(false));

            if ( interactive )
            {
                var mockAuthPage = new Mock<IPage>(MockBehavior.Strict);
                mockAuthPage.Setup(x => x.Url).Returns("https://login.microsoft.com/oauth");
                var mockAuthLocator = new Mock<ILocator>(MockBehavior.Strict);
                mockAuthLocator.Setup(x => x.Locator(".table-cell.content", null)).Returns(mockAuthLocator.Object);
                mockAuthLocator.Setup(x => x.First).Returns(mockAuthLocator.Object);
                mockAuthLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(true));
                mockAuthLocator.Setup(x => x.ClickAsync(null)).Returns(Task.CompletedTask);

                mockAuthPage.Setup(x => x.GetByRole(AriaRole.Button, null)).Returns(mockAuthLocator.Object);
                MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { mockAuthPage.Object });

                mockAuthPage.Setup(x => x.IsClosed).Returns(true);
            }

            string parameters = "";

            if ( ! string.IsNullOrEmpty(parameterName) )
            {
                var mockParameterLocator = new Mock<ILocator>(MockBehavior.Strict);
                parameters = $"{{'{parameterName}':'{parameterValue}'}}";
                MockPage.Setup(x => x.Locator($"[aria-label=\"{parameterName}\"]", null)).Returns(mockParameterLocator.Object);

                mockParameterLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);
                mockParameterLocator.Setup(x => x.FillAsync(parameterValue, null)).Returns(Task.CompletedTask);
            }

            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("Name", FormulaType.String, displayName: "Name"))
                .Add(new NamedFormulaType("Interactive", FormulaType.Boolean, displayName: "Interactive"))
                .Add(new NamedFormulaType("Parameters", FormulaType.String, displayName: "Parameters"));

            var rv1 = RecordValue.NewRecordFromFields(
                new NamedValue("Name", FormulaValue.New(name)),
                new NamedValue("Interactive", FormulaValue.New(interactive)),
                new NamedValue("Parameters", FormulaValue.New(parameters))
            );


            var table = TableValue.NewTable(recordType, rv1);

            var connectionHelper = new Mock<ConnectionHelper>(MockBehavior.Strict);
            connectionHelper.SetupSequence(x => x.Exists(MockBrowserContext.Object, "https://make.powerapps.com", name))
                .Returns(Task.FromResult(false))
                .Returns(Task.FromResult(true));


            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.GetConnectionHelper = () => connectionHelper.Object;

            // Act
            var result = function.Execute(table);

            // Assert
        }

        [Theory]
        [InlineData("Empty", "{}", "", "")]
        [InlineData("Single Quote Value", "{'test':'1'}", "test", "1")]
        [InlineData("Double Quote Value", "{\"test\":\"1\"}", "test", "1")]
        [InlineData("Spaces", "{\"Space in name\":\"1\"}", "Space in name", "1")]
        public async Task AddParamaters(string testCase, string json, string key, string value)
        {
            // Arrange
            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.Page = MockPage.Object;

            var name = testCase;

            if ( json.Length > 2 )
            {
                var mockLocator = new Mock<ILocator>();
                MockPage.Setup(x => x.Locator($"[aria-label=\"{key}\"]", null)).Returns(mockLocator.Object);
                mockLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.CompletedTask);
                mockLocator.Setup(x => x.FillAsync(value, null)).Returns(Task.CompletedTask);
            }

            // Act
            await function.AddParameters(json);

            // Assert
        }

        [Fact]
        public async Task WaitUntilCreatedEnabled()
        {
            // Arrange
            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.Page = MockPage.Object;

            var mockLocator = new Mock<ILocator>();
            MockPage.Setup(x => x.Locator(CreateConnectionFunction.CREATE_BUTTON_LOCATOR, null)).Returns(mockLocator.Object);

            mockLocator.SetupSequence(x => x.IsEnabledAsync(null))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            mockLocator.Setup(x => x.ClickAsync(null)).Returns(Task.CompletedTask);

            // Act
            await function.CreateConnection(10000);

            // Assert
        }

        [Fact]
        public async Task ThrowsExceptionIfNotEnabled()
        {
            // Arrange
            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.Page = MockPage.Object;

            var mockLocator = new Mock<ILocator>();
            MockPage.Setup(x => x.Locator(CreateConnectionFunction.CREATE_BUTTON_LOCATOR, null)).Returns(mockLocator.Object);

            mockLocator.SetupSequence(x => x.IsEnabledAsync(null))
                .ReturnsAsync(false);

            // Act
            await Assert.ThrowsAsync<Exception>(async () => await function.CreateConnection(0));

            // Assert
        }

        [Theory]
        [InlineData("")]
        [InlineData("http://make.powerapps.com")]
        [InlineData("http://make.powerapps.com,https://www.microsoft.com")]
        public async Task ThrowsExceptionIfAuthPageNotFound(string urls)
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            var pages = new List<IPage>();
            foreach (var url in urls.Split(',')) {
                var mockPage = new Mock<IPage>();
                mockPage.Setup(x => x.Url).Returns(url);
                pages.Add(mockPage.Object);
            }
            MockBrowserContext.Setup(x => x.Pages).Returns(pages);
            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
           
            // Act
            await Assert.ThrowsAsync<Exception>(async () => await function.HandleInteractiveLogin(0));

            // Assert
        }

        [Fact]
        public async Task ValidInteractiveLogin()
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            var pages = new List<IPage>();
            MockPage.Setup(x => x.Url).Returns("https://login.microsoft.com/oauth");
            pages.Add(MockPage.Object);
            MockBrowserContext.Setup(x => x.Pages).Returns(pages);
            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.Page = MockPage.Object;

            var mockAuthLocator = new Mock<ILocator>(MockBehavior.Strict);
            mockAuthLocator.Setup(x => x.Locator(".table-cell.content", null)).Returns(mockAuthLocator.Object);
            mockAuthLocator.Setup(x => x.First).Returns(mockAuthLocator.Object);
            mockAuthLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(true));
            mockAuthLocator.Setup(x => x.ClickAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.GetByRole(AriaRole.Button, null)).Returns(mockAuthLocator.Object);

            MockPage.Setup(x => x.IsClosed).Returns(true);

            // Act
            await function.HandleInteractiveLogin(0);

            // Assert
        }

        [Fact]
        public async Task ThrowExceptionIfInteractiveLoginNotClose()
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            var pages = new List<IPage>();
            MockPage.Setup(x => x.Url).Returns("https://login.microsoft.com/oauth");
            pages.Add(MockPage.Object);
            MockBrowserContext.Setup(x => x.Pages).Returns(pages);
            var function = new CreateConnectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            function.Page = MockPage.Object;

            var mockAuthLocator = new Mock<ILocator>(MockBehavior.Strict);
            mockAuthLocator.Setup(x => x.Locator(".table-cell.content", null)).Returns(mockAuthLocator.Object);
            mockAuthLocator.Setup(x => x.First).Returns(mockAuthLocator.Object);
            mockAuthLocator.Setup(x => x.WaitForAsync(null)).Returns(Task.FromResult(true));
            mockAuthLocator.Setup(x => x.ClickAsync(null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.GetByRole(AriaRole.Button, null)).Returns(mockAuthLocator.Object);

            MockPage.Setup(x => x.IsClosed).Returns(false);

            // Act
            await Assert.ThrowsAsync<Exception>(async () => await function.HandleInteractiveLogin(0));

            // Assert
        }
    }
}
