using System.Collections;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;
using testengine.module;
using Xunit;

namespace testengine.modules.simulation.tests
{
    public class SimulateConnectorFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ILogger> MockLogger;
        private Mock<IPage> MockPage;

        public SimulateConnectorFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
            MockPage = new Mock<IPage>(MockBehavior.Strict);
        }

        [Fact]
        public void CanCreate()
        {
            new SimulateConnectorFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
        }

        [Theory]
        [MemberData(nameof(QueryDataCases))]
        public async Task QueryData(TableValue value, long expectedCount)
        {
            // Arrange
            var function = new SimulateConnectorFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            var parameters = new List<NamedValue> { new NamedValue("Name", FormulaValue.New("test")),
                    new NamedValue("Then", value) };


            var sample = RecordValue.NewRecordFromFields(parameters);

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
            sampleRequest.Setup(req => req.Url).Returns("https://example.azure-apihub.net/invoke");
            sampleRequest.Setup(req => req.Headers).Returns(new Dictionary<string, string> { { "x-ms-request-url", $"/apim/test" } });
            sampleRequest.Setup(req => req.Method).Returns("POST");

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
            var responseValue = JsonConvert.DeserializeObject<Dictionary<string, object?>>(fulfillOptions.Body);
            Assert.Equal(expectedCount, GetCount(responseValue["value"]));
        }

        private static long GetCount(object obj)
        {
            if (obj is IEnumerable enumerable)
            {
                // Cast the IEnumerable to IEnumerable<object> and count the items
                return enumerable.Cast<object>().Count();
            }
            else
            {
                throw new ArgumentException("The provided object is not an IEnumerable.");
            }
        }

        public static IEnumerable<object[]> QueryDataCases()
        {
            var table = RecordType.Empty().Add(new NamedFormulaType("Name", StringType.String));
            var row = RecordValue.NewRecordFromFields(new NamedValue("Name", FormulaValue.New("Test")));

            yield return new object[] { TableValue.NewTable(table), 0 };
            yield return new object[] { TableValue.NewTable(table, row), 1 };
        }

        // TODO: Test Cases
        //
        // GET with Query Parameters
        // POST data with new Query paramaters
    }
}
