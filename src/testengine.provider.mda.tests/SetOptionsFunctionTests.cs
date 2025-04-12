// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;

namespace testengine.provider.mda.tests
{
    public class SetOptionsFunctionTests
    {

        [Theory]
        [InlineData("[]", 0)]
        [InlineData("[{\"Name\": \"Option1\", \"Value\": 1}]", 1)]
        [InlineData("[{\"Name\": \"Option1\", \"Value\": 1}, {\"Name\": \"Option2\", \"Value\": 2}]", 2)]
        public async Task ExecuteAsync(string jsonData, int expectedCount)
        {
            // Arrange
            var testInfraFunctionsMock = new Mock<ITestInfraFunctions>();
            var loggerMock = new Mock<ILogger>();
            var provider = new Mock<ITestWebProvider>();
            var function = new SetOptionsFunction(testInfraFunctionsMock.Object, loggerMock.Object);

            var pageMock = new Mock<IPage>();
            var javaScript = String.Empty;

            pageMock.Setup(p => p.EvaluateAsync<string>(It.IsAny<string>(), null))
                .Callback((string expression, object? value) => { javaScript = expression; });

            var pages = new List<IPage> { pageMock.Object };

            testInfraFunctionsMock.Setup(t => t.GetContext().Pages).Returns(pages);

            var recordType = RecordType.Empty()
                .Add("Value", NumberType.Number);

            var records = new List<RecordValue>();

            var data = JsonSerializer.Deserialize<List<ExpandoObject>>(jsonData);

            foreach (var item in data)
            {
                dynamic obj = (dynamic)item;
                records.Add(RecordValue.NewRecordFromFields(new NamedValue("Value", NumberValue.New(double.Parse(obj.Value.ToString())))));
            }
            var table = TableValue.NewTable(RecordType.Empty().Add("Value", NumberType.Number), records);

            var itemType = RecordType.Empty().Add("Name", FormulaType.String);
            var recordValue = new ControlRecordValue(itemType, provider.Object, "dev_test");

            // Act 
            var result = await function.ExecuteAsync(recordValue, table);

            // Assert
            var engine = new Jint.Engine();
            engine.Execute(@"
            var count = 0;
            var Xrm = {
                Page: {
                    ui: {
                        formContext: {
                            getAttribute: (name) => {
                                return {
                                    setValue: (values) => {
                                        count = values.length;
                                    }
                                }
                            }
                        }
                    }
                }
            }");

            Assert.Equal(0, engine.Evaluate("count"));

            engine.Execute(javaScript);

            Assert.Equal(expectedCount, engine.Evaluate("count"));
        }
    }
}
