// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;

namespace testengine.module
{
    public class SelectControlFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<IPage> MockPage;
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ILogger> MockLogger;
        private Mock<ILocator> MockLocator;

        public SelectControlFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockLocator = new Mock<ILocator>();
        }

        [Fact]
        public async Task HappyPathMatchIsFound()
        {
            // Arrange
            var function = new SelectControlFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            MockTestInfraFunctions.SetupGet(x => x.Page).Returns(MockPage.Object);
            MockPage.Setup(x => x.Locator("[data-control-name='Button1']", null)).Returns(MockLocator.Object);

            MockLocator.Setup(x => x.Nth(0)).Returns(MockLocator.Object);

            MockLocator.Setup(x => x.ClickAsync(null)).Returns(Task.CompletedTask);

            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");

            // Act & Assert
            function.Execute(recordValue, NumberValue.New((float)1.0));
        }
    }
}
