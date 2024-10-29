using Microsoft.Extensions.Logging;
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
        
        public SimulateDataverseFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void CanCreate()
        {
            new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
        }

        [Fact]
        public void Execute()
        {
            // Arrange
            var function = new SimulateDataverseFunction(MockTestInfraFunctions.Object, MockTestState.Object, MockLogger.Object);
            var recordType = RecordType.Empty()
                .Add("Action", StringType.String)
                .Add("Entity", StringType.String);
            var sample = RecordValue.NewRecordFromFields(recordType,
                    new NamedValue("Action", FormulaValue.New("Query")),
                    new NamedValue("Entity", FormulaValue.New("Test"))
                    );


            // Act
            function.Execute(sample);
        }
    }
}
