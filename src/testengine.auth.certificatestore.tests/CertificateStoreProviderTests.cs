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
            string username = Guid.NewGuid().ToString();
            X509Certificate2 mockCertificate;
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN={username}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
                X509Certificate2 certificate = provider.RetrieveCertificateForUser(username);

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
        public void RetrieveCertificateForUser_BySan_ReturnsCertificate()
        {
            string username = Guid.NewGuid().ToString();
            X509Certificate2 mockCertificate;
            using (var rsa = RSA.Create(2048))
            {
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(username);
                var sanExtension = sanBuilder.Build();
                var request = new CertificateRequest($"CN=extra-{username}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(sanExtension);
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
                X509Certificate2 certificate = provider.RetrieveCertificateForUser(username);

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
            string username = "nonexistentuser";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.RetrieveCertificateForUser(username));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RetrieveCertificateForUser_NullOrEmptyUsername_ThrowsArgumentException(string username)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.RetrieveCertificateForUser(username));
        }
    }
}
