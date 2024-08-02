using Moq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace testengine.auth.tests
{
    public class LocalUserCertificateProviderTests
    {
        [Fact]
        public void NameProperty_ShouldReturnLocalCert()
        {
            // Arrange
            var provider = new LocalUserCertificateProvider();

            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("localcert", name);
        }

        [Fact]
        public void Constructor_ShouldLoadCertificatesFromDirectory()
        {
            // Arrange
            var certDir = "LocalCertificates";
            Directory.CreateDirectory(certDir);
            var pfxFilePath = Path.Combine(certDir, "testcert.pfx");

            // Create a test certificate
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN=testcert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
                File.WriteAllBytes(pfxFilePath, certificate.Export(X509ContentType.Pfx));
            }

            // Act
            var provider = new LocalUserCertificateProvider();

            // Assert
            Assert.NotNull(provider.RetrieveCertificateForUser("CN=testcert"));

            // Cleanup
            Directory.Delete(certDir, true);
        }

        [Fact]
        public void RetrieveCertificateForUser_ShouldReturnNullForNonExistingUser()
        {
            // Arrange
            var provider = new LocalUserCertificateProvider();

            // Act
            var cert = provider.RetrieveCertificateForUser("nonexistinguser");

            // Assert
            Assert.Null(cert);
        }
    }
}
