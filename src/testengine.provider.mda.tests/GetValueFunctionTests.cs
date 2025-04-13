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
    public class GetValueFunctionTests
    {
        [Theory]
        [MemberData(nameof(ScalarTestValue))]
        public async Task ExecuteAsync_ShouldReturnExpectedScalar(string jsonData, FormulaValue expectedValue)
        {
            // Arrange
            var testInfraFunctionsMock = new Mock<ITestInfraFunctions>();
            var loggerMock = new Mock<ILogger>();
            var provider = new Mock<ITestWebProvider>();
            var function = new GetValueFunction(testInfraFunctionsMock.Object, loggerMock.Object);

            var pageMock = new Mock<IPage>();
            pageMock.Setup(p => p.EvaluateAsync<string>(It.IsAny<string>(), null))
                .Returns(Task.FromResult(jsonData));

            var pages = new List<IPage> { pageMock.Object };

            testInfraFunctionsMock.Setup(t => t.GetContext().Pages).Returns(pages);
            var recordType = RecordType.Empty().Add("Name", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, provider.Object, "dev_test");

            // Act
            var resultValue = await function.ExecuteAsync(recordValue);

            // Assert
            if (resultValue.TryGetPrimitiveValue(out object scalarValue))
            {
                if (expectedValue is BooleanValue booleanValue)
                {
                    Assert.Equal(booleanValue.Value.ToString(), scalarValue.ToString());
                }
            }
        }

        [Theory]
        [InlineData("[]", 0)]
        [InlineData("[1]", 1)]
        [InlineData("[2]", 1)]
        [InlineData("[1,2]", 2)]
        public async Task RecordOption(string valueJson, int expectedRecordCount)
        {
            // Arrange
            var testInfraFunctionsMock = new Mock<ITestInfraFunctions>();
            var loggerMock = new Mock<ILogger>();
            var provider = new Mock<ITestWebProvider>();
            var function = new GetValueFunction(testInfraFunctionsMock.Object, loggerMock.Object);

            var pageMock = new Mock<IPage>();
            pageMock.Setup(p => p.EvaluateAsync<string>(It.Is<string>(text => text.Contains("getValue")), null))
                .Returns(Task.FromResult(valueJson));

            pageMock.Setup(p => p.EvaluateAsync<string>(It.Is<string>(text => text.Contains("getOption")), null))
                .Returns(Task.FromResult("[{\"name\": \"foo\", value: 1}, {\"name\": \"foo2\", value: 2}]"));

            var pages = new List<IPage> { pageMock.Object };

            testInfraFunctionsMock.Setup(t => t.GetContext().Pages).Returns(pages);
            var recordType = RecordType.Empty().Add("Name", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, provider.Object, "dev_test");

            // Act
            var resultValue = await function.ExecuteAsync(recordValue);

            // Assert
            if (expectedRecordCount == 0)
            {
                Assert.IsAssignableFrom<BlankValue>(resultValue);
            } 
            else
            {
                Assert.IsAssignableFrom<TableValue>(resultValue);

                var tableValue = resultValue as TableValue;

                Assert.Equal(expectedRecordCount, tableValue.Rows.Count());
            } 
        }

        public static IEnumerable<object[]> ScalarTestValue()
        {
            yield return new object[] {
                "true",
                BooleanValue.New(true)
            };
            yield return new object[] {
                "false",
                BooleanValue.New(false)
            };
            yield return new object[] {
                "\"a\"",
                StringValue.New("a")
            };
            yield return new object[] {
                1,
                NumberValue.New(1)
            };
        }
        
}
}
