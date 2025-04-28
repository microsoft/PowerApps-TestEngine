// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Helpers
{
    public class AzureCliHelperTests
    {
        [Fact]
        public void GetAccessToken_HappyPath_ReturnsToken()
        {
            // Arrange
            var processMock = new Mock<IProcessWrapper>();
            processMock.Setup(p => p.StandardOutput).Returns("{\"accessToken\": \"test_token\"}");
            processMock.Setup(p => p.WaitForExit());

            var helper = new AzureCliHelper
            {
                ExecutableSuffix = () => ".cmd",
                ProcessStart = (info) => processMock.Object
            };

            var location = new Uri("https://example.com");

            // Act
            var token = helper.GetAccessToken(location);

            // Assert
            Assert.Equal("test_token", token);
        }

        [Fact]
        public void GetAccessToken_AzureCliNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var processMock = new Mock<IProcessWrapper>();
            processMock.Setup(p => p.StandardOutput).Returns(string.Empty);
            processMock.Setup(p => p.WaitForExit());

            var helper = new AzureCliHelper
            {
                ExecutableSuffix = () => ".cmd",
                ProcessStart = (info) => processMock.Object
            };

            var location = new Uri("https://example.com");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => helper.GetAccessToken(location));
        }

        [Fact]
        public void GetAccessToken_ProcessFails_ThrowsException()
        {
            // Arrange
            var helper = new AzureCliHelper
            {
                ExecutableSuffix = () => ".cmd",
                ProcessStart = (info) => null // Simulate process start failure
            };

            var location = new Uri("https://example.com");

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => helper.GetAccessToken(location));
        }

        [Fact]
        public void FindAzureCli_HappyPath_ReturnsPath()
        {
            // Arrange
            var processMock = new Mock<IProcessWrapper>();
            processMock.Setup(p => p.StandardOutput).Returns("C:\\path\\to\\az.cmd");
            processMock.Setup(p => p.WaitForExit());

            var helper = new AzureCliHelper
            {
                ProcessStart = (info) => processMock.Object
            };

            // Act
            var path = helper.FindAzureCli();

            // Assert
            Assert.Equal("C:\\path\\to\\az.cmd", path);
        }

        [Fact]
        public void FindAzureCli_AzureCliNotFound_ReturnsEmptyString()
        {
            // Arrange
            var processMock = new Mock<IProcessWrapper>();
            processMock.Setup(p => p.StandardOutput).Returns(string.Empty);
            processMock.Setup(p => p.WaitForExit());

            var helper = new AzureCliHelper
            {
                ProcessStart = (info) => processMock.Object
            };

            // Act
            var path = helper.FindAzureCli();

            // Assert
            Assert.Equal(string.Empty, path);
        }
    }
}
