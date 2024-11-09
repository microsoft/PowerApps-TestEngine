using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;

namespace testengine.provider.mda.tests
{
    public class ModelDrivenApplicationCanvasStateTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private JSObjectModel JsObjectModel;

        //adding this to reset the behavior after each test case for the assembly static function
        private readonly Func<Assembly> originalGetExecutingAssembly = () => typeof(ModelDrivenApplicationProvider).Assembly;

        const string GET_VARIABLES = "PowerAppsModelDrivenCanvas.getAppMagic().getLanguageRuntime().getVariableValuesJson()";


        public ModelDrivenApplicationCanvasStateTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockTestState.Setup(m => m.GetDomain()).Returns(String.Empty);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Loose);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            JsObjectModel = new JSObjectModel()
            {
                Controls = new List<JSControlModel>()
            };
        }


        // https://learn.microsoft.com/power-platform/power-fx/data-types
        public static IEnumerable<object[]> ScalarValuePowerFxStateData()
        {
            // Note PowerFX returns numeric values as decimal for int or long values

            TimeSpan timeSpan = new TimeSpan(11, 23, 45);
            DateTime midnight = DateTime.Today;
            DateTime timeOfDay = midnight.Add(timeSpan);
            double millisecondsSinceMidnight = (timeOfDay - midnight).TotalMilliseconds;

            yield return new object[] { "intValue", (decimal)1, null };
            yield return new object[] { "decimalNum", 3.14, null };
            // Assume dates are in UTC time not local timezone
            // TODO - Do we need ability to set default timezone?
            //yield return new object[] { "dateValue", (decimal)ConvertToUnixTimestamp(new DateTime(2024, 10, 1).ToUniversalTime()), null };
            //yield return new object[] { "dateTimeVal", (decimal)ConvertToUnixTimestamp(new DateTime(2024, 10, 1, 12, 34, 00).ToUniversalTime()), null };
            yield return new object[] { "boolFalse", false, null };
            yield return new object[] { "boolTrue", true, null };
            yield return new object[] { "colorValue", (double)PowerFxEvaluate("Color.Red"), (object expected, object actual) => expected.ToString().Equals(actual.ToString()) };
            yield return new object[] { "currencyvalue", (double)4.56, (object expected, object actual) => Math.Round((double)expected, 2) == Math.Round((double)actual, 2) };
            yield return new object[] { "GuidValue", new Guid("6557502c-5744-4f62-b89d-c7cead13c358"), null };
            yield return new object[] { "floatValue", 8.903e+121, null };
            yield return new object[] { "textValue", "Hello world", null };
            yield return new object[] { "recordValue", new { Company = "Northwind Traders", Staff = 35, NonProfit = false }, (object expected, object actual) => { return ConvertObjectToString(expected) == ConvertObjectToString(actual); } };
            yield return new object[] { "untypedValue", new { Field = 1234 }, (object expected, object actual) => { return ConvertObjectToString(expected) == ConvertObjectToString(actual); } };

            // TODO - Determinw how to covert Time() value into Power FX value
            // yield return new object[] { "timeValue", (decimal)millisecondsSinceMidnight, null };
        }

        public static long ConvertToUnixTimestamp(DateTime dateTime)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = dateTime - unixEpoch;
            return (long)timeSpan.TotalMilliseconds;
        }

        public static object PowerFxEvaluate(string statement, RecalcEngine? engine = null)
        {
            if (engine == null)
            {
                engine = new RecalcEngine();
            }
            return engine.Eval(statement).ToObject();
        }

        public static string ConvertObjectToString(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        [Theory]
        [MemberData(nameof(ScalarValuePowerFxStateData))]
        public void ScalarValuePowerFxState(string name, object value, object? evaluator)
        {
            // Arrange
            var json = GetData("sample");
            MockTestState = new Mock<ITestState>();
            MockTestState.Setup(m => m.GetDomain()).Returns("https://localhost/?type=custom");
            MockTestState.SetupSet(m => m.ExecuteStepByStep = true);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            var powerAppFunctions = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var engine = new RecalcEngine();

            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(json));

            var args = new TestStepEventArgs { Engine = engine, Result = BlankValue.NewVoid(), StepNumber = 1, TestStep = "Set(name,'Test')" };

            // Act
            MockTestState.Raise(m => m.BeforeTestStepExecuted += null, args);
            var gotValue = engine.TryGetValue(name, out FormulaValue powerFXValue);

            // Assert
            Assert.True(gotValue);
            powerFXValue.TryGetPrimitiveValue(out object primativeValue);

            if (primativeValue == null)
            {
                primativeValue = powerFXValue.ToObject();
            }

            if (evaluator != null)
            {
                var validate = (Func<object, object, bool>)evaluator;
                Assert.True(validate(value, primativeValue));
            }
            else
            {
                Assert.Equal(value, primativeValue);
            }

        }


        [Fact]
        public void CollectionDataExist()
        {
            // Arrange
            var json = GetData("sample");
            MockTestState = new Mock<ITestState>();
            MockTestState.Setup(m => m.GetDomain()).Returns("https://localhost/?type=custom");
            MockTestState.SetupSet(m => m.ExecuteStepByStep = true);

            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            var powerAppFunctions = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var engine = new RecalcEngine();

            MockTestInfraFunctions.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(json));

            var args = new TestStepEventArgs { Engine = engine, Result = BlankValue.NewVoid(), StepNumber = 1, TestStep = "Set(name,'Test')" };

            // Act
            MockTestState.Raise(m => m.BeforeTestStepExecuted += null, args);
            var gotValue = engine.TryGetValue("data", out FormulaValue powerFXValue);

            // Assert
            Assert.True(gotValue);
            Assert.Equal("1", PowerFxEvaluate("CountRows(data)", engine).ToString());
        }

        [Fact]
        public async Task HandleNewVariableInProviderCopiedToExisting()
        {
            // Arrange
            var existingEngine = new RecalcEngine();
            var providerEngine = new RecalcEngine();
            var existingState = new ModelDrivenApplicationCanvasState();
            var newState = new ModelDrivenApplicationCanvasState();

            var existingInfra = new Mock<ITestInfraFunctions>();
            var newInfra = new Mock<ITestInfraFunctions>();

            var existingJson = GetData("empty");
            var newJson = GetData("variable-int");


            existingInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(existingJson));
            newInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(newJson));

            // Act
            await existingState.UpdateRecalcEngine(existingInfra.Object, new TestStepEventArgs { Engine = existingEngine });
            await newState.UpdateRecalcEngine(newInfra.Object, new TestStepEventArgs { Engine = providerEngine });
            await newState.ApplyChanges(newInfra.Object, existingState, providerEngine, existingEngine);

            // Assert
            Assert.Equal("1", existingState.VariableState["intValue"]);
        }

        [Fact]
        public async Task DoesNotCopyValueFromExistingValueThatDoesNotExistInProvider()
        {
            // Arrange
            var existingEngine = new RecalcEngine();
            var providerEngine = new RecalcEngine();
            var existingState = new ModelDrivenApplicationCanvasState();
            var newState = new ModelDrivenApplicationCanvasState();

            var existingInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            var newInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);

            var existingJson = GetData("variable-int");
            var newJson = GetData("empty");


            existingInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(existingJson));
            newInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(newJson));

            // Act
            await existingState.UpdateRecalcEngine(existingInfra.Object, new TestStepEventArgs { Engine = existingEngine });
            await newState.UpdateRecalcEngine(newInfra.Object, new TestStepEventArgs { Engine = providerEngine });
            await newState.ApplyChanges(newInfra.Object, existingState, providerEngine, existingEngine);

            // Assert
            Assert.False(newState.VariableState.ContainsKey("intValue"));
        }

        [Fact]
        public async Task ExistingValueIsUpdatedInProvider()
        {
            // Arrange
            var existingEngine = new RecalcEngine();
            var providerEngine = new RecalcEngine();
            var existingState = new ModelDrivenApplicationCanvasState();
            var newState = new ModelDrivenApplicationCanvasState();

            var existingInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            var newInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);

            var existingJson = GetData("variable-int");
            var newJson = GetData("variable-int2");


            existingInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(existingJson));
            newInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(newJson));

            // Act
            await existingState.UpdateRecalcEngine(existingInfra.Object, new TestStepEventArgs { Engine = existingEngine });
            await newState.UpdateRecalcEngine(newInfra.Object, new TestStepEventArgs { Engine = providerEngine });
            await newState.ApplyChanges(newInfra.Object, existingState, providerEngine, existingEngine);

            // Assert
            Assert.Equal("2", existingState.VariableState["intValue"]);
        }

        [Fact]
        public async Task ExistingValueIsUpdatedCopyNewValueToProvider()
        {
            // Arrange
            var existingEngine = new RecalcEngine();
            var providerEngine = new RecalcEngine();
            var existingState = new ModelDrivenApplicationCanvasState();
            var newState = new ModelDrivenApplicationCanvasState();

            var existingInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            var newInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);

            var existingJson = GetData("variable-int2");
            var newJson = GetData("variable-int");


            existingInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(existingJson));
            newInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(newJson));
            newInfra.Setup(x => x.RunJavascriptAsync<string>($"PowerAppsModelDrivenCanvas.getAppMagic().getLanguageRuntime().setScopeVariableValue('1','intValue', 2)")).Returns(Task.FromResult("{}"));

            // Act
            await existingState.UpdateRecalcEngine(existingInfra.Object, new TestStepEventArgs { Engine = existingEngine });
            await newState.UpdateRecalcEngine(newInfra.Object, new TestStepEventArgs { Engine = providerEngine });
            await newState.ApplyChanges(newInfra.Object, existingState, providerEngine, existingEngine);

            // Assert
            Assert.Equal("1", newState.VariableState["intValue"]);
        }

        [Fact]
        public async Task NewCollectionInProviderCopyValueToExisting()
        {
            // Arrange
            var existingEngine = new RecalcEngine();
            var providerEngine = new RecalcEngine();
            var existingState = new ModelDrivenApplicationCanvasState();
            var newState = new ModelDrivenApplicationCanvasState();

            var existingInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            var newInfra = new Mock<ITestInfraFunctions>(MockBehavior.Strict);

            var existingJson = GetData("empty");
            var newJson = GetData("collection");

            existingInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(existingJson));
            newInfra.Setup(x => x.RunJavascriptAsync<string>(GET_VARIABLES)).Returns(Task.FromResult(newJson));

            // Act
            await existingState.UpdateRecalcEngine(existingInfra.Object, new TestStepEventArgs { Engine = existingEngine });
            await newState.UpdateRecalcEngine(newInfra.Object, new TestStepEventArgs { Engine = providerEngine });
            await newState.ApplyChanges(newInfra.Object, existingState, providerEngine, existingEngine);

            // Assert
            Assert.Equal("[{\"Name\":\"Test Item\"}]", newState.CollectionState["data"]);
            Assert.Equal("[{\"Name\":\"Test Item\"}]", existingState.CollectionState["data"]);
        }

        private string GetData(string name)
        {
            return new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"testengine.provider.mda.tests.data-{name}.json")).ReadToEnd();
        }

    }
}
