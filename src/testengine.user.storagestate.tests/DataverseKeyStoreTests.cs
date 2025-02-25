// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using Xunit;

namespace testengine.user.storagestate.tests
{
    public class DataverseKeyStoreTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IOrganizationService> _mockService;
        private readonly string _friendlyName;
        private readonly DataverseKeyStore _dataverseKeyStore;

        public DataverseKeyStoreTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockService = new Mock<IOrganizationService>();
            _friendlyName = "testFriendlyName";
            _dataverseKeyStore = new DataverseKeyStore(_mockLogger.Object, _mockService.Object, _friendlyName);
        }

        [Fact]
        public void GetAllElements_ReturnsElements_WhenKeysExist()
        {
            // Arrange
            var xmlString = "<Key>Test</Key>";
            var entity = new Entity("te_key") { ["te_xml"] = xmlString };
            var entityCollection = new EntityCollection(new List<Entity> { entity });
            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<QueryExpression>())).Returns(entityCollection);

            // Act
            var result = _dataverseKeyStore.GetAllElements();

            // Assert
            Assert.Single(result);
            Assert.Equal(xmlString, result.First().ToString());
        }

        [Fact]
        public void GetAllElements_ReturnsEmpty_WhenNoKeysExist()
        {
            // Arrange
            var entityCollection = new EntityCollection(new List<Entity>());
            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<QueryExpression>())).Returns(entityCollection);

            // Act
            var result = _dataverseKeyStore.GetAllElements();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void StoreElement_CreatesEntity_WhenCalled()
        {
            // Arrange
            var element = new XElement("Key", "Test");
            _mockService.Setup(s => s.Create(It.IsAny<Entity>())).Returns(Guid.NewGuid());

            // Act
            _dataverseKeyStore.StoreElement(element, _friendlyName);

            // Assert
            _mockService.Verify(s => s.Create(It.Is<Entity>(e =>
                e["te_name"].ToString() == _friendlyName &&
                e["te_xml"].ToString() == element.ToString(SaveOptions.DisableFormatting))), Times.Once);
        }

        [Fact]
        public void StoreElement_ThrowsException_WhenServiceFails()
        {
            // Arrange
            var element = new XElement("Key", "Test");
            _mockService.Setup(s => s.Create(It.IsAny<Entity>())).Throws(new Exception("Service failure"));

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _dataverseKeyStore.StoreElement(element, _friendlyName));
            Assert.Equal("Service failure", exception.Message);
        }
    }
}
