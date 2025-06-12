using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SelectDepartmentOptionsFunctionTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidDepartment_CallsSelectDepartmentOptionsAsyncAndLogs()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDepartmentOptionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New("HR");

            // Act
            var result = await func.ExecuteAsync(department);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDepartmentOptionsAsync("HR"), Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing SelectDepartmentOptionsFunction for department 'HR'.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SelectDepartmentOptionsFunction execution completed.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void Execute_CallsAsyncSynchronously()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDepartmentOptionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New("Finance");

            // Act
            var result = func.Execute(department);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDepartmentOptionsAsync("Finance"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_NullTestInfraFunctions_ThrowsNullReferenceException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockLogger = new Mock<ILogger>();
            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns((ITestInfraFunctions)null);

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New("IT");

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => func.ExecuteAsync(department));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyDepartment_CallsSelectDepartmentOptionsAsyncWithEmptyString()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDepartmentOptionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New(string.Empty);

            // Act
            var result = await func.ExecuteAsync(department);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDepartmentOptionsAsync(string.Empty), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhitespaceDepartment_CallsSelectDepartmentOptionsAsyncWithWhitespace()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDepartmentOptionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New("   ");

            // Act
            var result = await func.ExecuteAsync(department);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDepartmentOptionsAsync("   "), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_NullDepartment_ThrowsArgumentNullException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => func.ExecuteAsync(null));
        }

        [Fact]
        public async Task ExecuteAsync_SelectDepartmentOptionsAsyncReturnsFalse_ReturnsTrue()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDepartmentOptionsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New("Legal");

            // Act
            var result = await func.ExecuteAsync(department);

            // Assert
            // The function always returns true, regardless of the async result
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDepartmentOptionsAsync("Legal"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SelectDepartmentOptionsAsyncThrows_PropagatesException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDepartmentOptionsAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            var func = new SelectDepartmentOptionsFunction(mockWebProvider.Object, mockLogger.Object);
            var department = StringValue.New("Admin");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => func.ExecuteAsync(department));
        }
    }
}
