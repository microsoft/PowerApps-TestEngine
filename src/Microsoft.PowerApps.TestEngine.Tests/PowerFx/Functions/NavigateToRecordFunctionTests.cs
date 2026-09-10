using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class NavigateToRecordFunctionTests
    {
        [Fact]
        public async Task ExecuteAsync_NavigatesToExistingRecord_ReturnsTrue()
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

            // Simulate entityId found
            mockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null))
                .ReturnsAsync("entity123");

            mockPage.Setup(x => x.EvaluateAsync<bool>(It.IsAny<string>(), null))
                .ReturnsAsync(true);

            bool updateModelCalled = false;
            Task UpdateModel() { updateModelCalled = true; return Task.CompletedTask; }

            var func = new NavigateToRecordFunction(
                mockWebProvider.Object,
                UpdateModel,
                mockLogger.Object);

            // Act
            var result = await func.ExecuteAsync(
                FormulaValue.New("account") as StringValue,
                FormulaValue.New("entityrecord") as StringValue,
                NumberValue.New(1.0));

            // Assert
            Assert.True(((BooleanValue)result).Value);
            Assert.True(updateModelCalled);
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Navigating to existing record")),
                (Exception)null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_NavigatesToNewRecord_ReturnsTrue()
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

            // Simulate no entityId found
            mockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null))
                .ReturnsAsync(string.Empty);

            mockPage.Setup(x => x.EvaluateAsync<bool>(It.IsAny<string>(), null))
                .ReturnsAsync(true);

            bool updateModelCalled = false;
            Task UpdateModel() { updateModelCalled = true; return Task.CompletedTask; }

            var func = new NavigateToRecordFunction(
                mockWebProvider.Object,
                UpdateModel,
                mockLogger.Object);

            // Act
            var result = await func.ExecuteAsync(
                FormulaValue.New("contact") as StringValue,
                FormulaValue.New("entityrecord") as StringValue,
                NumberValue.New(1.0));

            // Assert
            Assert.True(((BooleanValue)result).Value);
            Assert.True(updateModelCalled);
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No selected entity found")),
                (Exception)null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public void Execute_CallsAsyncSynchronously()
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

            mockPage.Setup(x => x.EvaluateAsync<string>(It.IsAny<string>(), null))
                .ReturnsAsync("entity456");
            mockPage.Setup(x => x.EvaluateAsync<bool>(It.IsAny<string>(), null))
                .ReturnsAsync(true);

            var func = new NavigateToRecordFunction(
                mockWebProvider.Object,
                () => Task.CompletedTask,
                mockLogger.Object);

            // Act
            var result = func.Execute(
                FormulaValue.New("lead") as StringValue,
                FormulaValue.New("entityrecord") as StringValue,
                NumberValue.New(1.0));

            // Assert
            Assert.True(((BooleanValue)result).Value);
        }
    }
}
