using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Moq;
using Xunit;

namespace testengine.auth.certificatestore.tests
{
    public class CertificateStoreProviderTests
    {
        private readonly CertificateStoreProvider provider;

        public CertificateStoreProviderTests()
        {
            provider = new CertificateStoreProvider();
        }

        [Fact]
        public void NameProperty_ShouldReturnLocalCert()
        {
            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("certstore", name);
        }

        [Fact]
        public void RetrieveCertificateForUser_BySubjectName_ReturnsCertificate()
        {
            // Arrange
            string userSubjectName = $"CN={Guid.NewGuid().ToString()}";
            X509Certificate2 mockCertificate;
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(userSubjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                mockCertificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            }
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(mockCertificate);
                    store.Close();
                }

                // Act
                X509Certificate2 certificate = provider.RetrieveCertificateForUser(userSubjectName);

                // Assert
                Assert.NotNull(certificate);
                Assert.Equal(mockCertificate, certificate);

            }
            finally
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Remove(mockCertificate);
                    store.Close();
                }
            }
        }

        [Fact]
        public void RetrieveCertificateForUser_CertificateNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            string userSubjectName = "nonexistentuser";

            // Act & Assert
            Assert.Null(provider.RetrieveCertificateForUser(userSubjectName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RetrieveCertificateForUser_NullOrEmptyUsername_ThrowsArgumentException(string? userSubjectName)
        {
            // Act & Assert
            Assert.Null(provider.RetrieveCertificateForUser(userSubjectName));
        }
    }
}
