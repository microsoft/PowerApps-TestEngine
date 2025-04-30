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
    public class GetOptionsFunctionTests
    {

        [Theory]
        [InlineData("[]", 0)]
        [InlineData("[{\"text\": \"Option1\", \"value\": 1}]", 1)]
        [InlineData("[{\"text\": \"Option1\", \"value\": 1}, {\"text\": \"Option2\", \"value\": 2}]", 2)]
        public async Task ExecuteAsync_ShouldReturnExpectedTableValue(string jsonData, int expectedCount)
        {
            // Arrange
            var testInfraFunctionsMock = new Mock<ITestInfraFunctions>();
            var loggerMock = new Mock<ILogger>();
            var provider = new Mock<ITestWebProvider>();
            var function = new GetOptionsFunction(testInfraFunctionsMock.Object, loggerMock.Object);

            var pageMock = new Mock<IPage>();
            pageMock.Setup(p => p.EvaluateAsync<string>(It.IsAny<string>(), null)).ReturnsAsync(jsonData);

            var pages = new List<IPage> { pageMock.Object };

            testInfraFunctionsMock.Setup(t => t.GetContext().Pages).Returns(pages);
            var recordType = RecordType.Empty().Add("Name", FormulaType.String);
            var recordValue = new ControlRecordValue(recordType, provider.Object, "dev_test");

            // Act
            var result = await function.ExecuteAsync(recordValue);

            // Assert
            Assert.IsAssignableFrom<TableValue>(result);

            var tableResult = result as TableValue;

            var results = tableResult.Rows.ToList();

            Assert.Equal(expectedCount, results.Count());
        }
    }
}
