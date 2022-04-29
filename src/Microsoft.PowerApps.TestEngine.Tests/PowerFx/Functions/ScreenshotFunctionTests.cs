// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Core.Public.Values;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class ScreenshotFunctionTests 
    {
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IFileSystem> MockFileSystem;

        public ScreenshotFunctionTests()
        {
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
        }

        [Fact]
        public void ScreenshotFunctionThrowsOnInvalidResultDirectoryTest()
        {
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns("");
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(false);
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            Assert.Throws<InvalidOperationException>(() => screenshotFunction.Execute(FormulaValue.New("screenshot.png")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("C:\\folder")]
        [InlineData("test.txt")]
        [InlineData("test.img")]
        [InlineData("C:\\folder\\test.png")]
        public void ScreenshotFunctionThrowsOnInvalidScreenshotNameTest(string screenshotName)
        {
            var testResultDirectory = "C:\\testResults";
            MockSingleTestInstanceState.Setup(x => x.GetTestResultsDirectory()).Returns(testResultDirectory);
            MockFileSystem.Setup(x => x.IsValidFilePath(It.IsAny<string>())).Returns(true);
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            Assert.Throws<ArgumentException>(() => screenshotFunction.Execute(FormulaValue.New(screenshotName)));
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
            MockTestInfraFunctions.Setup(x => x.ScreenshotAsync(It.IsAny<string>())).Returns(Task.FromResult(0));
            var screenshotFunction = new ScreenshotFunction(MockTestInfraFunctions.Object, MockSingleTestInstanceState.Object, MockFileSystem.Object);
            screenshotFunction.Execute(FormulaValue.New(screenshotName));

            MockSingleTestInstanceState.Verify(x => x.GetTestResultsDirectory(), Times.Once());
            MockFileSystem.Verify(x => x.IsValidFilePath(testResultDirectory), Times.Once());
            MockTestInfraFunctions.Verify(x => x.ScreenshotAsync(Path.Combine(testResultDirectory, screenshotName)), Times.Once());
        }
    }
}