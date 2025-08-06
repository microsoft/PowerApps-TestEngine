// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class AssertJavaScriptFunctionTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IOrganizationService> _mockOrgService;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ITestState> _mockTestState;
        private readonly string _testFilePath;
        private readonly string _testFileContent;
        public AssertJavaScriptFunctionTests()
        {
            _mockLogger = new Mock<ILogger>(MockBehavior.Strict);
            _mockOrgService = new Mock<IOrganizationService>(MockBehavior.Strict);
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockTestState = new Mock<ITestState>(MockBehavior.Loose);

            // Create a temporary JavaScript file for testing
            _testFilePath = Path.GetTempFileName() + ".js";
            _testFileContent = "// Test JavaScript file\nfunction testFunction() { return 'hello'; }";
            File.WriteAllText(_testFilePath, _testFileContent);

            // Setup the test state with Preview namespace enabled for all tests
            SetupTestStateWithPreviewNamespace();
        }

        private void SetupTestStateWithPreviewNamespace()
        {
            var testSettings = new TestSettings
            {
                ExtensionModules = new TestSettingExtensions
                {
                    AllowPowerFxNamespaces = new HashSet<string> { "Preview" }
                }
            };

            _mockTestState.Setup(s => s.GetTestSettings()).Returns(testSettings);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidRunAndExpected_ReturnsSuccess()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);
            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Run"] = StringValue.New("'test' + 'success'"),
                ["Expected"] = StringValue.New("testsuccess")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());
            // Act
            var result = await function.ExecuteAsync(record);
            // Assert
            Assert.IsAssignableFrom<BlankValue>(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingRunParameter_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);
            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Expected"] = StringValue.New("testsuccess")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("Run", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingExpectedParameter_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Run"] = StringValue.New("'test' + 'success'")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("Expected", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidRunCode_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Run"] = StringValue.New("invalidFunction()"),
                ["Expected"] = StringValue.New("result")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("JavaScript test code execution failed", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithSetupCode_ExecutesCorrectly()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);
            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Setup"] = StringValue.New("var x = 10; var y = 5;"),
                ["Run"] = StringValue.New("x + y"),
                ["Expected"] = StringValue.New("15")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act
            var result = await function.ExecuteAsync(record);

            // Assert
            Assert.IsAssignableFrom<BlankValue>(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidSetupCode_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Setup"] = StringValue.New("var x = nonExistentFunction();"),
                ["Run"] = StringValue.New("10"),
                ["Expected"] = StringValue.New("10")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("Setup code execution failed", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithLocalFile_ExecutesCorrectly()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            // Setup mock file system
            _mockFileSystem.Setup(fs => fs.FileExists(_testFilePath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(_testFilePath)).Returns(_testFileContent);

            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Location"] = StringValue.New(_testFilePath),
                ["Run"] = StringValue.New("testFunction()"),
                ["Expected"] = StringValue.New("hello")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act
            var result = await function.ExecuteAsync(record);

            // Assert
            Assert.IsAssignableFrom<BlankValue>(result);

            // Verify file system was called
            _mockFileSystem.Verify(fs => fs.FileExists(_testFilePath), Times.Once);
            _mockFileSystem.Verify(fs => fs.ReadAllText(_testFilePath), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithNonExistentFile_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            // Setup mock file system
            string nonExistentPath = "C:/nonexistent/file.js";
            _mockFileSystem.Setup(fs => fs.FileExists(nonExistentPath)).Returns(false);

            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Location"] = StringValue.New(nonExistentPath),
                ["Run"] = StringValue.New("10"),
                ["Expected"] = StringValue.New("10")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("Could not read JavaScript file", exception.Message);

            // Verify file system was called
            _mockFileSystem.Verify(fs => fs.FileExists(nonExistentPath), Times.Once);
            _mockFileSystem.Verify(fs => fs.ReadAllText(nonExistentPath), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithWebResource_ExecutesCorrectly()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            // Setup mock org service to return a web resource
            var webResourceContent = Convert.ToBase64String(global::System.Text.Encoding.UTF8.GetBytes(
                "function webResourceFunction() { return 'from webresource'; }"));

            var mockEntity = new Entity("webresource");
            mockEntity["content"] = webResourceContent;
            var mockEntityCollection = new EntityCollection();
            mockEntityCollection.Entities.Add(mockEntity);

            // Create query expression variable for Moq setup
            _mockOrgService.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(q =>
                q.EntityName == "webresource" &&
                q.ColumnSet.Columns.Contains("content") && q.ColumnSet.Columns.Contains("name"))))
                .Returns(mockEntityCollection);

            var function = new AssertJavaScriptFunction(_mockLogger.Object, _mockOrgService.Object, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["WebResource"] = StringValue.New("new_testscript.js"),
                ["Run"] = StringValue.New("webResourceFunction()"),
                ["Expected"] = StringValue.New("from webresource")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act
            var result = await function.ExecuteAsync(record);

            // Assert
            Assert.IsAssignableFrom<BlankValue>(result);

            // Verify service was called with the proper query expression
            _mockOrgService.Verify(s => s.RetrieveMultiple(It.Is<QueryExpression>(q =>
                q.EntityName == "webresource" &&
                q.ColumnSet.Columns.Contains("content") &&
                q.ColumnSet.Columns.Contains("name"))), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithNonExistentWebResource_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            // Setup mock org service to return empty collection
            _mockOrgService.Setup(s => s.RetrieveMultiple(It.Is<QueryExpression>(q =>
                q.EntityName == "webresource" &&
                q.ColumnSet.Columns.Contains("content") && q.ColumnSet.Columns.Contains("name"))))
                .Returns(new EntityCollection());

            var function = new AssertJavaScriptFunction(_mockLogger.Object, _mockOrgService.Object, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["WebResource"] = StringValue.New("nonexistent.js"),
                ["Run"] = StringValue.New("10"),
                ["Expected"] = StringValue.New("10")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("Could not retrieve web resource", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailingAssertion_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Run"] = StringValue.New("'actual'"),
                ["Expected"] = StringValue.New("expected")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("JavaScript assertion failed", exception.Message);
            Assert.Contains("Expected 'expected', got 'actual'", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithComplexJavaScript_ExecutesCorrectly()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);
            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);
            var complexJs = @"
                var numbers = [1, 2, 3, 4, 5];
                var sum = numbers.reduce(function(a, b) { return a + b; }, 0);
                var doubled = numbers.map(function(n) { return n * 2; });
                doubled.join(',')
            ";

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Run"] = StringValue.New(complexJs),
                ["Expected"] = StringValue.New("2,4,6,8,10")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act
            var result = await function.ExecuteAsync(record);

            // Assert
            Assert.IsAssignableFrom<BlankValue>(result);
        }

        [Fact]
        public async Task ExecuteAsync_FileSystemUsage_VerifyMockCalls()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            string testPath = "C:/test/script.js";
            string testContent = "function testMock() { return 'mocked content'; }";

            // Setup detailed mock expectations
            _mockFileSystem.Setup(fs => fs.FileExists(testPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(testPath)).Returns(testContent);
            _mockFileSystem.Setup(fs => fs.CanAccessFilePath(testPath)).Returns(true);

            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Location"] = StringValue.New(testPath),
                ["Run"] = StringValue.New("testMock()"),
                ["Expected"] = StringValue.New("mocked content")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act
            var result = await function.ExecuteAsync(record);

            // Assert
            Assert.IsAssignableFrom<BlankValue>(result);

            // Verify that file system methods were called with expected parameters
            _mockFileSystem.Verify(fs => fs.FileExists(testPath), Times.Once);
            _mockFileSystem.Verify(fs => fs.ReadAllText(testPath), Times.Once);
            // Verify that other file system methods were NOT called
            // We can't use It.IsAny for methods with optional parameters in expression trees
            // so we need to specify all parameters explicitly
            _mockFileSystem.Verify(fs => fs.WriteTextToFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _mockFileSystem.Verify(fs => fs.Delete(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithFileSystemError_ThrowsException()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            string testPath = "C:/test/error.js";

            // Setup mock to throw an exception when reading the file
            _mockFileSystem.Setup(fs => fs.FileExists(testPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(testPath)).Throws(new IOException("Simulated file error"));

            var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

            var recordFields = new Dictionary<string, FormulaValue>
            {
                ["Location"] = StringValue.New(testPath),
                ["Run"] = StringValue.New("var x = 10;"),
                ["Expected"] = StringValue.New("10")
            };
            var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                new NamedValue(kv.Key, kv.Value)).ToArray());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AssertionFailureException>(async () =>
                await function.ExecuteAsync(record));
            Assert.Contains("Could not read JavaScript file", exception.Message);

            // Verify that methods were called correctly
            _mockFileSystem.Verify(fs => fs.FileExists(testPath), Times.Once);
            _mockFileSystem.Verify(fs => fs.ReadAllText(testPath), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithRelativePathInSetup_ResolvesCorrectly()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            // Setup mock test state with test config file
            var mockTestConfigFile = new FileInfo(Path.Combine(Path.GetTempPath(), "test_config.yaml"));
            _mockTestState.Setup(ts => ts.GetTestConfigFile()).Returns(mockTestConfigFile);

            // Create a test JS file in the same directory as the mock test config
            var relativeJsFile = "setup_script.js";
            var fullJsFilePath = Path.Combine(Path.GetTempPath(), relativeJsFile);
            var setupContent = "var setupVar = 'from_setup_file';";

            try
            {
                File.WriteAllText(fullJsFilePath, setupContent);

                // Setup mock file system
                _mockFileSystem.Setup(fs => fs.FileExists(fullJsFilePath)).Returns(true);
                _mockFileSystem.Setup(fs => fs.ReadAllText(fullJsFilePath)).Returns(setupContent);

                var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

                var recordFields = new Dictionary<string, FormulaValue>
                {
                    ["Setup"] = StringValue.New(relativeJsFile),
                    ["Run"] = StringValue.New("setupVar"),
                    ["Expected"] = StringValue.New("from_setup_file")
                };
                var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                    new NamedValue(kv.Key, kv.Value)).ToArray());

                // Act
                var result = await function.ExecuteAsync(record);

                // Assert
                Assert.IsAssignableFrom<BlankValue>(result);

                // Verify file system was called with the correct full path
                _mockFileSystem.Verify(fs => fs.FileExists(fullJsFilePath), Times.Once);
                _mockFileSystem.Verify(fs => fs.ReadAllText(fullJsFilePath), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(fullJsFilePath))
                {
                    File.Delete(fullJsFilePath);
                }
            }
        }

        [Fact]
        public async Task ExecuteAsync_WithJavaScriptFileInSetup_LoadsAndExecutesCorrectly()
        {
            // Arrange
            LoggingTestHelper.SetupMock(_mockLogger);

            // Create a temporary JavaScript file for setup code
            var setupFilePath = Path.GetTempFileName() + ".js";
            var setupFileContent = "// Setup JavaScript file\nvar setupVar = 42;";
            File.WriteAllText(setupFilePath, setupFileContent);

            try
            {
                // Setup mock file system
                _mockFileSystem.Setup(fs => fs.FileExists(setupFilePath)).Returns(true);
                _mockFileSystem.Setup(fs => fs.ReadAllText(setupFilePath)).Returns(setupFileContent);

                var function = new AssertJavaScriptFunction(_mockLogger.Object, null, _mockFileSystem.Object, _mockTestState.Object);

                var recordFields = new Dictionary<string, FormulaValue>
                {
                    ["Setup"] = StringValue.New(setupFilePath),
                    ["Run"] = StringValue.New("setupVar"),
                    ["Expected"] = StringValue.New("42")
                };
                var record = RecordValue.NewRecordFromFields(recordFields.Select(kv =>
                    new NamedValue(kv.Key, kv.Value)).ToArray());

                // Act
                var result = await function.ExecuteAsync(record);

                // Assert
                Assert.IsAssignableFrom<BlankValue>(result);

                // Verify file system was called with the correct path
                _mockFileSystem.Verify(fs => fs.FileExists(setupFilePath), Times.Once);
                _mockFileSystem.Verify(fs => fs.ReadAllText(setupFilePath), Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(setupFilePath))
                {
                    File.Delete(setupFilePath);
                }
            }
        }

        // Cleanup temp file after tests
        public void Dispose()
        {
            try
            {
                if (File.Exists(_testFilePath))
                {
                    File.Delete(_testFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log exception but don't throw during cleanup
                Console.WriteLine($"Error in cleanup: {ex.Message}");
            }
        }
    }
}
