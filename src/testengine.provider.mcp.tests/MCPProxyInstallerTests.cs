// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.System;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Providers.Tests
{
    public class MCPProxyInstallerTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<IProcessRunner> _mockProcessRunner;
        private readonly Mock<ILogger> _mockLogger;
        private readonly MCPProxyInstaller _installer;
        private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        public MCPProxyInstallerTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockProcessRunner = new Mock<IProcessRunner>();
            _mockLogger = new Mock<ILogger>();
            _installer = new MCPProxyInstaller(_mockFileSystem.Object, _mockProcessRunner.Object, _mockLogger.Object);
            _installer.WriteFile = (file, content) => _files.TryAdd(file, content);
        }

        [Fact]
        public void EnsureMCPProxyInstalled_CreatesMCPDirectory_WhenItDoesNotExist()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.GetDefaultRootTestEngine()).Returns("C:\\TestEngine");
            _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

            // Act
            _installer.EnsureMCPProxyInstalled();

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateDirectory("C:\\TestEngine\\mcp"), Times.Once);
        }

        [Fact]
        public void EnsureMCPProxyInstalled_ExtractsFiles_WhenTheyDoNotExist()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.GetDefaultRootTestEngine()).Returns("C:\\TestEngine");
            _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("file content");

            // Act
            _installer.EnsureMCPProxyInstalled();

            // Assert
            Assert.Contains("C:\\TestEngine\\mcp\\app.js", _files.Keys);
            Assert.Contains("C:\\TestEngine\\mcp\\app.js.hash", _files.Keys);
            Assert.Contains("C:\\TestEngine\\mcp\\package.json", _files.Keys);
         }

        [Fact]
        public void EnsureMCPProxyInstalled_RunsNpmInstall_WhenFilesAreExtracted()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.GetDefaultRootTestEngine()).Returns("C:\\TestEngine");
            _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("file content");
            _mockProcessRunner.Setup(pr => pr.Run("npm", "install", "C:\\TestEngine\\mcp"))
                              .Returns(0);

            // Act
            _installer.EnsureMCPProxyInstalled();

            // Assert
            _mockProcessRunner.Verify(pr => pr.Run("npm", "install", "C:\\TestEngine\\mcp"), Times.Once);
        }

        [Fact]
        public void EnsureMCPProxyInstalled_ThrowsException_WhenNpmInstallFails()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.GetDefaultRootTestEngine()).Returns("C:\\TestEngine");
            _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("file content");
            _mockProcessRunner.Setup(pr => pr.Run("npm", "install", "C:\\TestEngine\\mcp"))
                              .Returns(1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _installer.EnsureMCPProxyInstalled());
            Assert.Contains("npm install failed", exception.Message);
        }

        [Fact]
        public void EnsureMCPProxyInstalled_DoesNotRunNpmInstall_WhenFilesAlreadyExist()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.GetDefaultRootTestEngine()).Returns("C:\\TestEngine");
            _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText("C:\\TestEngine\\mcp\\app.js.hash")).Returns(MCPProxyInstaller.ComputeEmbeddedResourceHash("proxy/app.js"));

            // Act
            _installer.EnsureMCPProxyInstalled();

            // Assert
            _mockProcessRunner.Verify(pr => pr.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
