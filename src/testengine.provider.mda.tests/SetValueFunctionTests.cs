// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;

namespace testengine.provider.mda.tests
{
    public class SetValueFunctionTests
    {

        [Theory]
        [MemberData(nameof(SetValueData))]
        public async Task ExecuteAsync(TableValue table, int expectedCount)
        {
            // Arrange
            var testInfraFunctionsMock = new Mock<ITestInfraFunctions>();
            var loggerMock = new Mock<ILogger>();
            var provider = new Mock<ITestWebProvider>();
            var function = new SetValueFunction(testInfraFunctionsMock.Object, loggerMock.Object);

            var pageMock = new Mock<IPage>();
            var javaScript = new List<string>();

            pageMock.Setup(p => p.EvaluateAsync<string>(It.IsAny<string>(), null))
                .Callback((string expression, object? value) => { javaScript.Add(expression); });

            var pages = new List<IPage> { pageMock.Object };

            testInfraFunctionsMock.Setup(t => t.GetContext().Pages).Returns(pages);

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
                                    },
                                    fireOnChange: () => {}
                                }
                            },
                        }
                    }
                }
            }");

            Assert.Equal(0, engine.Evaluate("count"));

            foreach (var text in javaScript)
            {
                engine.Execute(text);
            }

            Assert.Equal(expectedCount, engine.Evaluate("count"));
        }

        public static IEnumerable<object[]> SetValueData()
        {
            yield return new object[] { TableValue.NewTable(RecordType.Empty(), new List<RecordValue>()), 0 };

            yield return new object[] { CreateTestTableValue("Name", FormulaType.String, "A"), 1 };

            yield return new object[] { CreateTestTableValue("Value", FormulaType.Number, 1.0), 1 };

            yield return new object[] { CreateTestTableValue("Value", FormulaType.Number, 1), 1 };

            yield return new object[] { CreateTestTableValue("Value", FormulaType.Guid, Guid.NewGuid()), 1 };

            yield return new object[] { CreateTestTableValue("Value", FormulaType.DateTime, DateTime.Now), 1 };
        }

        private static TableValue CreateTestTableValue(string name, FormulaType formulaType, object scalarValue)
        {
            FormulaValue value = FormulaValue.NewBlank();

            if (scalarValue is string stringValue)
            {
                value = FormulaValue.New(stringValue);
            }

            if (scalarValue is int intValue)
            {
                value = FormulaValue.New((double)intValue);
            }

            if (scalarValue is double doubleValue)
            {
                value = FormulaValue.New(doubleValue);
            }

            if (scalarValue is DateTime dateTimeValue)
            {
                value = FormulaValue.New(dateTimeValue);
            }

            return TableValue.NewTable(RecordType.Empty().Add(name, formulaType), new List<RecordValue>() {
                RecordValue.NewRecordFromFields(new NamedValue(name, value )) });
        }
    }
}
