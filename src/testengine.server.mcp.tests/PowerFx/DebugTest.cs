// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.MCP.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

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
        public void Debug_ExportFacts()
        {
            // Arrange
            var saveFactFunction = new ScanStateManager.SaveFactFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);
                
            var exportFactsFunction = new ScanStateManager.ExportFactsFunction(
                _mockFileSystem.Object,
                _mockLogger.Object,
                _testWorkspacePath);

            // Add some app facts first
            var screenFact = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Screens")),
                new NamedValue("Key", FormulaValue.New("Screen1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.pa.yaml")),
                new NamedValue("Value", FormulaValue.New("Main Screen"))
            );

            var controlFact = RecordValue.NewRecordFromFields(
                new NamedValue("Category", FormulaValue.New("Controls")),
                new NamedValue("Key", FormulaValue.New("Button1")),
                new NamedValue("AppPath", FormulaValue.New("TestApp.pa.yaml")),
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

            saveFactFunction.Execute(screenFact);
            saveFactFunction.Execute(controlFact);

            // Act
            var exportParams = RecordValue.NewRecordFromFields(
                new NamedValue("AppPath", FormulaValue.New("TestApp.pa.yaml"))
            );
            
            var result = exportFactsFunction.Execute(exportParams);

            // Assert
            Assert.True(result.Value);

            // Check if any file writes were captured
            Assert.NotEmpty(writeParameters);

            // Verify facts file was written with correct parameters
            _mockFileSystem.Verify(
                fs => fs.WriteTextToFile(
                    It.Is<string>(path => path.Contains("TestApp.app-facts.json")),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.AtLeastOnce());
        }
    }
}
