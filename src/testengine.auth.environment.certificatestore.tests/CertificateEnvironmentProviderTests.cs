// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.PowerApps.TestEngine.System;
using Moq;

namespace testengine.auth.certificatestore.tests
{
    public class CertificateEnvironmentProviderTests
    {
        private readonly CertificateEnvironmentProvider provider;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;

        public CertificateEnvironmentProviderTests()
        {
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            provider = new CertificateEnvironmentProvider(MockEnvironmentVariable.Object);
        }

        [Fact]
        public void NameProperty_ShouldReturnLocalCert()
        {
            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("certenv", name);
        }

        [Fact]
        public void RetrieveCertificateForUser_BySubjectName_ReturnsCertificate()
        {
            // Arrange
            string userSubjectName = $"TEST_USER";
            X509Certificate2 mockCertificate;
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest("CN=" + userSubjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                mockCertificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            }
            // Export the certificate to a byte array
            byte[] rawData = mockCertificate.Export(X509ContentType.Cert);

            // Convert the byte array to a base64 string
            string base64Encoded = Convert.ToBase64String(rawData);

            MockEnvironmentVariable.Setup(x => x.GetVariable(userSubjectName)).Returns(base64Encoded);

            // Act
            X509Certificate2 certificate = provider.RetrieveCertificateForUser(userSubjectName);

            // Assert
            Assert.NotNull(certificate);
            Assert.Equal(mockCertificate, certificate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RetrieveCertificateForUser_NullOrEmptyUsername_ThrowsArgumentException(string? userSubjectName)
        {
            // Act & Assert
            Assert.Null(provider.RetrieveCertificateForUser(userSubjectName));
        }

        [Theory]
        [InlineData("nonexistentuser", "")]
        [InlineData("nonexistentuser", null)]
        public void RetrieveCertificateForUser_NotFound_ThrowsArgumentException(string? userSubjectName, string? variableValue)
        {
            // Arrange
            MockEnvironmentVariable.Setup(x => x.GetVariable(userSubjectName)).Returns(variableValue);

            // Act & Assert
            Assert.Null(provider.RetrieveCertificateForUser(userSubjectName));
        }
    }
}
