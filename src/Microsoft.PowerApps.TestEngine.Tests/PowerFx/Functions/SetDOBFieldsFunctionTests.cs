using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SetDOBFieldsFunctionTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidInputs_ExecutesJavaScriptAndReturnsTrue()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();
            var mockPage = new Mock<IPage>();
            var mockContext = new Mock<IBrowserContext>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.GetContext()).Returns(mockContext.Object);
            mockContext.Setup(x => x.Pages).Returns(new[] { mockPage.Object });

            bool jsCalled = false;
            mockPage.Setup(x => x.EvaluateAsync(It.IsAny<string>(), null))
                .Callback<string, object>((js, arg) => jsCalled = true)
                .Returns(Task.FromResult((JsonElement?)default));

            var func = new SetDOBFieldsFunction(
                mockWebProvider.Object,
                mockLogger.Object);

            // Act
            var result = await func.ExecuteAsync(FormulaValue.New("2023-01-01") as StringValue, FormulaValue.New("12:00:00") as StringValue);

            // Assert
            Assert.True(result.Value);
            Assert.True(jsCalled);
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing SetDOBFieldsFunction")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void Execute_ValidInputs_CallsAsyncSynchronously()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();
            var mockPage = new Mock<IPage>();
            var mockContext = new Mock<IBrowserContext>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.GetContext()).Returns(mockContext.Object);
            mockContext.Setup(x => x.Pages).Returns(new[] { mockPage.Object });

            mockPage.Setup(x => x.EvaluateAsync(It.IsAny<string>(), null))
                .Returns(Task.FromResult((JsonElement?)default));

            var func = new SetDOBFieldsFunction(
                mockWebProvider.Object,
                mockLogger.Object);

            // Act
            var result = func.Execute(FormulaValue.New("2023-01-01") as StringValue, FormulaValue.New("12:00:00") as StringValue);

            // Assert
            Assert.True(result.Value);
        }

        [Fact]
        public async Task ExecuteAsync_NoPages_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();
            var mockContext = new Mock<IBrowserContext>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.GetContext()).Returns(mockContext.Object);
            mockContext.Setup(x => x.Pages).Returns(Array.Empty<IPage>());

            var func = new SetDOBFieldsFunction(
                mockWebProvider.Object,
                mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                func.ExecuteAsync(FormulaValue.New("2023-01-01") as StringValue, FormulaValue.New("12:00:00") as StringValue));
        }
    }
}
