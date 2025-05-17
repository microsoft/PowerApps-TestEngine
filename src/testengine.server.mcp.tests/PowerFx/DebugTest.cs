// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx.Types;
using Moq;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    public class DebugTest
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testWorkspacePath;

        public DebugTest()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _testWorkspacePath = Path.Combine(Path.GetTempPath(), "TestWorkspace");
        }

        [Fact]
        public void Debug_GenerateUIMap()
        {
            // Arrange
            var wrapper = new SaveInsightWrapper(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            // Add some UI-related insights first
            var screenInsight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Screens")),
                new NamedValue("Key", FormulaValue.New("Screen1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("Main Screen"))
            );
            
            var controlInsight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Controls")),
                new NamedValue("Key", FormulaValue.New("Button1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("Button Control"))
            );
            
            // Setup file system to capture file write parameters
            var writeParameters = new List<(string path, string content, bool overwrite)>();
            _mockFileSystem
                .Setup(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, string, bool>((path, content, overwrite) => 
                {
                    writeParameters.Add((path, content, overwrite));
                    Console.WriteLine($"WriteTextToFile called with: {path}, content length: {content?.Length ?? 0}, overwrite: {overwrite}");
                });
                
            wrapper.Execute(screenInsight);
            wrapper.Execute(controlInsight);

            // Act
            var result = wrapper.GenerateUIMap("TestApp.msapp");

            // Assert
            Assert.True(result.Value);
            
            // Check if any file writes were captured
            Assert.NotEmpty(writeParameters);
            
            // Verify UI map was written with correct parameters
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.msapp.ui-map")),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)),
                Times.AtLeastOnce());
        }
    }
}
