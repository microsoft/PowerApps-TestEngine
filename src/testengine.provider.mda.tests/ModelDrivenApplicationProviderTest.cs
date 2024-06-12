// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    public class ModelDrivenApplicationProviderTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<ILogger> MockLogger;
        private JSObjectModel JsObjectModel;

        public ModelDrivenApplicationProviderTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Loose);
            JsObjectModel = new JSObjectModel()
            {
                Controls = new List<JSControlModel>()
            };
        }

        [Theory]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Text\"}",    ""                                , "'ABC'", "ABC" )]
        [InlineData("{\"controlName\":\"Label1\",\"index\":null,\"parentControl\":null,\"propertyName\":\"Visible\"}", "[{Key:'Visible',Value: 'False'}]", ""     , "False")]

        public void GetPropertyValueValues(string itemPathString, string json, object inputValue, object expectedOutput)
        {
            // Arrange
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);
            var itemPath = JsonConvert.DeserializeObject<ItemPath>(itemPathString);
            var inputPropertyValue = "{PropertyValue:" + inputValue + "}";

            MockTestState.Setup(m => m.GetTimeout()).Returns(30000);
            MockSingleTestInstanceState.Setup(m => m.GetLogger()).Returns(MockLogger.Object);

            if (string.IsNullOrEmpty(json))
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(string.Format(ModelDrivenApplicationProvider.QueryFormField, itemPath.ControlName)))
                    .Returns(Task.FromResult(inputPropertyValue));
            } 
            else
            {
                MockTestInfraFunctions.Setup(m => m.RunJavascriptAsync<string>(string.Format(ModelDrivenApplicationProvider.ControlPropertiesQuery, itemPath.ControlName)))
                    .Returns(Task.FromResult(json));
            }

            // Act
            var provider = new ModelDrivenApplicationProvider(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockTestState.Object);
            var result = provider.GetPropertyValueFromControl<string>(itemPath);

            // Assert
            var propertryValue = JsonConvert.DeserializeObject<JSPropertyValueModel>(result);

            Assert.Equal(expectedOutput, propertryValue.PropertyValue);
        }
    }
}
