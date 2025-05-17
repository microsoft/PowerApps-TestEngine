// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;
using Moq.Language;
using Moq.Language.Flow;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.MCP.Tests.PowerFx
{
    public class SaveInsightWrapperTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testWorkspacePath;

        public SaveInsightWrapperTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _testWorkspacePath = Path.Combine(Path.GetTempPath(), "TestWorkspace");
        }

        [Fact]
        public void Execute_CallsUnderlyingSaveInsightFunction()
        {
            // Arrange
            var wrapper = new SaveInsightWrapper(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
            
            var insight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("TestCategory")),
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );

            // Act
            var result = wrapper.Execute(insight);

            // Assert
            Assert.True(result.Value);
              // We can't directly verify that the underlying function was called
            // since it's created inside the wrapper, but we can verify the insight is added
            // by calling Flush and checking that files were written.
            wrapper.Flush("TestApp.msapp");            // Verify at least one file write call was made
            // Use Times.AtLeastOnce() to avoid expression tree issues with optional parameters
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public void Flush_CallsFlushInsightsFunction()
        {
            // Arrange
            var wrapper = new SaveInsightWrapper(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            // Add an insight first
            var insight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("TestCategory")),
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );            wrapper.Execute(insight);

            // Mock the file system to capture file paths
            var filePathCapture = new List<string>();
            _mockFileSystem
                .Setup(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, string, bool>((path, _, _) => filePathCapture.Add(path));

            // Act
            var result = wrapper.Flush("TestApp.msapp");

            // Assert
            Assert.True(result.Value); // Verify test insights file was written
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)), // Explicitly specify the optional argument
                Times.AtLeastOnce());            // Verify the test insights file was written with the correct path
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.msapp.test-insights.json")),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)),
                Times.AtLeastOnce());
        }

        [Fact]
        public void GenerateUIMap_CallsGenerateUIMapFunction()
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
            );            wrapper.Execute(screenInsight);

            // Mock the file system to capture file paths
            var filePathCapture = new List<string>();
            _mockFileSystem
                .Setup(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback<string, string, bool>((path, _, _) => filePathCapture.Add(path));

            // Act
            var result = wrapper.GenerateUIMap("TestApp.msapp");

            // Assert
            Assert.True(result.Value);            // Verify UI map was written
            // Use Times.AtLeastOnce() to avoid expression tree issues with optional parameters
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)), // Explicitly specify the optional argument
                Times.AtLeastOnce());            // Verify the UI map file was written with the correct path - includes .msapp in file name
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.msapp.ui-map.json")),
                    It.IsAny<string>(),
                    It.Is<bool>(overwrite => overwrite == false)),
                Times.AtLeastOnce());
        }


        [Fact]
        public void Execute_HandlesExceptions_AndReturnsFalse()
        {
            // Arrange
            // Configure mock to throw exception on any file write
            // Update the Setup call to explicitly pass the optional argument
            _mockFileSystem
                .Setup(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new IOException("Simulated error"));


            var wrapper = new SaveInsightWrapper(
                _mockFileSystem.Object, 
                _mockLogger.Object, 
                _testWorkspacePath);
            
            var insight = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("TestCategory")),
                new NamedValue("Key", FormulaValue.New("TestKey")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.msapp")),
                new NamedValue("Value", FormulaValue.New("TestValue"))
            );

            // Save multiple insights to trigger a file write
            for (int i = 0; i < 15; i++)
            {
                wrapper.Execute(insight);
            }
            
            // Act - This should trigger the exception in the underlying function
            var result = wrapper.Flush("TestApp.msapp");

            // Assert
            Assert.False(result.Value);            // Verify error was logged
            // Use Times.AtLeastOnce() to avoid expression tree issues with optional parameters
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce());
        }
    }
}
