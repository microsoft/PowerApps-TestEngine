// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
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
    public class SelectSectionFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IPage> MockPage;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;

        public SelectSectionFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
        }

        [Fact]
        public void SectionFound()
        {
            // Arrange
            MockTestInfraFunctions.Setup(x => x.GetContext()).Returns(MockBrowserContext.Object);
            MockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.Setup(x => x.Url).Returns("https://make.powerapps.com/environments/a1234567-1111-2222-4444-555566667777/home");
            MockPage.Setup(x => x.WaitForSelectorAsync("[data-test-id='solutions']:visible", It.IsAny<PageWaitForSelectorOptions>())).ReturnsAsync(new Mock<IElementHandle>().Object);
            MockPage.Setup(x => x.ClickAsync("[data-test-id='solutions']", null)).Returns(Task.CompletedTask);

            var function = new SelectSectionFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            // Act
            function.Execute(FormulaValue.New("solutions"));

            // Assert
        }

    }
}
