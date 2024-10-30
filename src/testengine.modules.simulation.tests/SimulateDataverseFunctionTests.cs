using System.Collections;
using System.Dynamic;
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

        [Theory]
        [MemberData(nameof(QueryDataCases))]
        public async Task QueryData(TableValue value, long expectedCount)
        {
            // Arrange
            var function = new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            var parameters = new List<NamedValue> { new NamedValue("Action", FormulaValue.New("Query")),
                    new NamedValue("Entity", FormulaValue.New("accounts")),
                    new NamedValue("Then", value) };


            var sample = RecordValue.NewRecordFromFields(parameters);

            MockTestInfraFunctions.SetupGet(m => m.Page).Returns(MockPage.Object);

            Func<IRoute, Task> registedCallback = null;

            MockPage.Setup(page => page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null))
               .Returns(Task.CompletedTask)
               .Callback<string, Func<IRoute, Task>, PageRouteOptions>((url, callback, options) =>
               {
                   if (!url.Contains("$batch"))
                   {
                       // Record the call back so we can interact with it
                       registedCallback = callback;
                   }
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
            var responseValue = JsonConvert.DeserializeObject<Dictionary<string, object?>>(fulfillOptions.Body);

            Assert.Equal(expectedCount, responseValue["@odata.count"]);
            Assert.Equal(expectedCount, responseValue["@Microsoft.Dynamics.CRM.totalrecordcount"]);
            Assert.Equal(expectedCount, GetCount(responseValue["value"]));
        }

        [Theory]
        [MemberData(nameof(QueryDataCases))]
        public async Task BatchData(TableValue value, long expectedCount)
        {
            // Arrange
            var function = new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            var parameters = new List<NamedValue> { new NamedValue("Action", FormulaValue.New("Query")),
                    new NamedValue("Entity", FormulaValue.New("accounts")),
                    new NamedValue("Then", value) };


            var sample = RecordValue.NewRecordFromFields(parameters);

            MockTestInfraFunctions.SetupGet(m => m.Page).Returns(MockPage.Object);

            Func<IRoute, Task> registedCallback = null;

            MockPage.Setup(page => page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null))
               .Returns(Task.CompletedTask)
               .Callback<string, Func<IRoute, Task>, PageRouteOptions>((url, callback, options) =>
               {
                   if (url.Contains("$batch"))
                   {
                       // Record the call back so we can interact with it
                       registedCallback = callback;
                   }
               });

            var mockRoute = new Mock<IRoute>();

            var sb = new StringBuilder(@"--batch_a1234567-1111-2222-3333-44445555666
Content-Type: application/http
Content-Transfer-Encoding: binary

GET accounts?%24select=accountid%2Caccountnumber%2Centityimage%2Cemailaddress1%2Centityimage_timestamp&%24count=true HTTP/1.1
Accept: application/json
Prefer: odata.maxpagesize=100,odata.include-annotations=*

--batch_a1234567-1111-2222-3333-44445555666--");

            var sampleRequest = new Mock<IRequest>();
            sampleRequest.Setup(req => req.Url).Returns("https://example.com/api/data/v9.0/$batch");
            sampleRequest.Setup(req => req.Method).Returns("POST");
            sampleRequest.Setup(req => req.PostData).Returns(sb.ToString());

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

            var json = String.Empty;
            using (var reader = new StringReader(fulfillOptions.Body))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("{"))
                    {
                        json = line;
                    }
                }
            }

            Assert.NotEqual(String.Empty, json);

            var responseValue = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json);

            Assert.Equal(expectedCount, responseValue["@odata.count"]);
            Assert.Equal(expectedCount, responseValue["@Microsoft.Dynamics.CRM.totalrecordcount"]);
            Assert.Equal(expectedCount, GetCount(responseValue["value"]));
        }

        [Theory]
        [MemberData(nameof(QueryDataCases))]
        public async Task BatchDataCount(TableValue value, long expectedCount)
        {
            // Arrange
            var function = new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);

            var parameters = new List<NamedValue> { new NamedValue("Action", FormulaValue.New("Query")),
                    new NamedValue("Entity", FormulaValue.New("accounts")),
                    new NamedValue("Then", value) };


            var sample = RecordValue.NewRecordFromFields(parameters);

            MockTestInfraFunctions.SetupGet(m => m.Page).Returns(MockPage.Object);

            Func<IRoute, Task> registedCallback = null;

            MockPage.Setup(page => page.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null))
               .Returns(Task.CompletedTask)
               .Callback<string, Func<IRoute, Task>, PageRouteOptions>((url, callback, options) =>
               {
                   if (url.Contains("$batch"))
                   {
                       // Record the call back so we can interact with it
                       registedCallback = callback;
                   }
               });

            var mockRoute = new Mock<IRoute>();

            var sb = new StringBuilder(@"--batch_31b8538b-fdf5-4d97-b78f-83517f4ef163
Content-Type: application/http
Content-Transfer-Encoding: binary

GET accounts?%24select=accountid%2Caccountnumber%2Centityimage%2Cemailaddress1%2Centityimage_timestamp&%24count=true HTTP/1.1
Accept: application/json
Prefer: odata.maxpagesize=100,odata.include-annotations=*

--batch_31b8538b-fdf5-4d97-b78f-83517f4ef163--");

            var sampleRequest = new Mock<IRequest>();
            sampleRequest.Setup(req => req.Url).Returns("https://example.com/api/data/v9.0/$batch");
            sampleRequest.Setup(req => req.Method).Returns("POST");
            sampleRequest.Setup(req => req.PostData).Returns(sb.ToString());

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

            var json = String.Empty;
            using (var reader = new StringReader(fulfillOptions.Body))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("{"))
                    {
                        json = line;
                    }
                }
            }

            Assert.NotEqual(String.Empty, json);

            var responseValue = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Assert.Equal(expectedCount, responseValue["@Microsoft.Dynamics.CRM.totalrecordcount"]);
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
        // GET Data (Query)
        // - When clause match to $filter
        // - When clause match to fetchXml
        // - Retreive single record
        //
        // UPDATE data with PATCH
        // POST data with new Records
    }
}
