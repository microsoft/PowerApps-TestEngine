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
    public class SelectDropdownOptionFunctionTests
    {
        [Fact]
        public async Task ExecuteAsync_ValidDropdown_CallsOptionAsyncAndLogs()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDropdownOptionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New("HR");

            // Act
            var result = await func.ExecuteAsync(dropdownOption);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDropdownOptionAsync("HR"), Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing SelectDropdownOptionFunction for dropdown 'HR'.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SelectDropdownOptionFunction execution completed.")),
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
            mockTestInfra.Setup(x => x.SelectDropdownOptionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New("Finance");

            // Act
            var result = func.Execute(dropdownOption);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDropdownOptionAsync("Finance"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_NullTestInfraFunctions_ThrowsNullReferenceException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockLogger = new Mock<ILogger>();
            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns((ITestInfraFunctions)null);

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New("IT");

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => func.ExecuteAsync(dropdownOption));
        }

        [Fact]
        public async Task ExecuteAsync_EmptyDropdown_CallsSelectDropdownOptionAsyncWithEmptyString()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDropdownOptionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New(string.Empty);

            // Act
            var result = await func.ExecuteAsync(dropdownOption);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDropdownOptionAsync(string.Empty), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhitespaceDropdown_CallsSelectDropdownOptionAsyncWithWhitespace()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDropdownOptionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(true));

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New("   ");

            // Act
            var result = await func.ExecuteAsync(dropdownOption);

            // Assert
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDropdownOptionAsync("   "), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_NullDropdown_ThrowsArgumentNullException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => func.ExecuteAsync(null));
        }

        [Fact]
        public async Task ExecuteAsync_SelectDropdownOptionAsyncReturnsFalse_ReturnsTrue()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDropdownOptionAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(false));

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New("Legal");

            // Act
            var result = await func.ExecuteAsync(dropdownOption);

            // Assert
            // The function always returns true, regardless of the async result
            Assert.True(result.Value);
            mockTestInfra.Verify(x => x.SelectDropdownOptionAsync("Legal"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_SelectDropdownOptionAsyncThrows_PropagatesException()
        {
            // Arrange
            var mockWebProvider = new Mock<ITestWebProvider>();
            var mockTestInfra = new Mock<ITestInfraFunctions>();
            var mockLogger = new Mock<ILogger>();

            mockWebProvider.SetupGet(x => x.TestInfraFunctions).Returns(mockTestInfra.Object);
            mockTestInfra.Setup(x => x.SelectDropdownOptionAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            var func = new SelectDropdownOptionFunction(mockWebProvider.Object, mockLogger.Object);
            var dropdownOption = StringValue.New("Admin");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => func.ExecuteAsync(dropdownOption));
        }
    }
}
