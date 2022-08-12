// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class ScreenshotFunctionTests
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<ILogger> MockLogger;

        public ScreenshotFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void ScreenshotFunctionThrowsOnInvalidResultDirectoryTest()
        {
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns("");
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            LoggingTestHelper.SetupMock(MockLogger);
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object, MockLogger.Object);
            Assert.Throws<InvalidOperationException>(() => screenshotFunction.Execute(FormulaValue.New("screenshot.png")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("C:\\folder")]
        [InlineData("test.txt")]
        [InlineData("test.img")]
        public void ScreenshotFunctionThrowsOnInvalidScreenshotNameTest(string screenshotName)
        {
            var testResultDirectory = "C:\\testResults";
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(testResultDirectory);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            LoggingTestHelper.SetupMock(MockLogger);
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object, MockLogger.Object);
            Assert.Throws<ArgumentException>(() => screenshotFunction.Execute(FormulaValue.New(screenshotName)));
        }


        [Fact]
        public void ScreenshotFunctionThrowsOnNonRelativeFilePathTest()
        {
            var testResultDirectory = "C:\\testResults";
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(testResultDirectory);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            LoggingTestHelper.SetupMock(MockLogger);
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object, MockLogger.Object);
            Assert.Throws<ArgumentException>(() => screenshotFunction.Execute(FormulaValue.New(Path.Combine(Path.GetFullPath(Directory.GetCurrentDirectory()), "screeshot.jpg"))));
        }

        [Theory]
        [InlineData("screenshot.png")]
        [InlineData("screenshot.jpg")]
        [InlineData("screenshot.jpeg")]
        public void ScreenshotFunctionTest(string screenshotName)
        {
            var testResultDirectory = "C:\\testResults";
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(testResultDirectory);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            MockTestInfraFunctions.Setup(x => x.ScreenshotAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            LoggingTestHelper.SetupMock(MockLogger);
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object, MockLogger.Object);
            screenshotFunction.Execute(FormulaValue.New(screenshotName));

            MockSingleTestInstanceState.Verify(x => x.GetTestResultsDirectory(), Times.Once());
            MockFileSystem.Verify(x => x.IsValidFilePath(testResultDirectory), Times.Once());
            MockTestInfraFunctions.Verify(x => x.ScreenshotAsync(Path.Combine(testResultDirectory, screenshotName)), Times.Once());
        }
    }
}
