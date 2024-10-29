using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using testengine.module;
using Xunit;

namespace testengine.modules.simulation.tests
{
    public class SimulateDataverseFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ILogger> MockLogger;
        private Mock<IPage> MockPage;
        
        public SimulateDataverseFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockPage = new Mock<IPage>(MockBehavior.Strict);
        }

        [Fact]
        public void CanCreate()
        {
            new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
        }

        [Fact]
        public async Task Execute()
        {
            // Arrange
            var function = new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            var table = RecordType.Empty().Add(new NamedFormulaType("Name", StringType.String));
            var row = RecordValue.NewRecordFromFields(new NamedValue("Name", FormulaValue.New("Test")));

            var sample = RecordValue.NewRecordFromFields(
                    new NamedValue("Action", FormulaValue.New("Query")),
                    new NamedValue("Entity", FormulaValue.New("accounts")),
                    new NamedValue("Then", TableValue.NewTable(table, row))
            );

            MockTestInfraFunctions.SetupGet(m => m.Page).Returns(MockPage.Object);

            Func<IRoute, Task> registedCallback = null;

            MockPage.Setup(page => page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null))
               .Returns(Task.CompletedTask)
               .Callback<string, Func<IRoute, Task>, PageRouteOptions>((url, callback, options) =>
               {
                   // Record the call back so we can interact with it
                   registedCallback = callback;
               });

            var mockRoute = new Mock<IRoute>();

            var sampleRequest = new Mock<IRequest>();
            sampleRequest.Setup(req => req.Url).Returns("https://example.com/api/data/v9.0/accounts?param=value");
            sampleRequest.Setup(req => req.Method).Returns("GET");

            mockRoute.SetupGet(m => m.Request).Returns(sampleRequest.Object);

            RouteFulfillOptions fulfillOptions = null;
            mockRoute.Setup(m => m.FulfillAsync(It.IsAny<RouteFulfillOptions>()))
                .Callback((RouteFulfillOptions options) =>
                {
                    fulfillOptions = options;
                })
                .Returns(Task.CompletedTask);

            // Act
            function.Execute(sample);

            Assert.NotNull(registedCallback);
            await registedCallback(mockRoute.Object);

            // Assert
            Assert.Equal(200, fulfillOptions.Status);
        }

        // TODO: Test Cases
        //
        // GET Data (Query)
        // - When clause match to $filter
        // - When clause match to fetchXml
        // - Retreive single record
        //
        // UPDATE data with PATCH
        // POST data with new Records
    }
}
