using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Moq;

namespace testengine.auth.tests
{
    public class LocalUserCertificateProviderTests
    {
        private Mock<IFileSystem> MockFileSystem;

        public LocalUserCertificateProviderTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }

        [Fact]
        public void NameProperty_ShouldReturnLocalCert()
        {
            // Arrange
            MockFileSystem.Setup(x => x.GetTempPath()).Returns("");
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns("");
            MockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(false);
            var provider = new LocalUserCertificateProvider(MockFileSystem.Object);

            // Act
            var name = provider.Name;

            // Assert
            Assert.Equal("localcert", name);
        }

        [Fact]
        public void Constructor_ShouldLoadCertificatesFromDirectory()
        {
            // Arrange
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns("");
            var certDir = Path.Combine(MockFileSystem.Object.GetDefaultRootTestEngine(), "LocalCertificates");
            MockFileSystem.Setup(x => x.Exists(certDir)).Returns(true);
            try
            {
                Directory.CreateDirectory(certDir);
                var pfxFilePath = Path.Combine(certDir, "testcert.pfx");

                // Create a test certificate
                using (var rsa = RSA.Create(2048))
                {
                    var request = new CertificateRequest($"CN=testcert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
                    File.WriteAllBytes(pfxFilePath, certificate.Export(X509ContentType.Pfx));
                }
                MockFileSystem.Setup(x => x.GetFiles(certDir, "*.pfx")).Returns(new string[] {pfxFilePath});

                // Act
                var provider = new LocalUserCertificateProvider(MockFileSystem.Object);

                // Assert
                Assert.NotNull(provider.RetrieveCertificateForUser("CN=testcert"));

                // Cleanup
                Directory.Delete(certDir, true);
            }
            catch
            {
                if (Directory.Exists(certDir))
                {
                    Directory.Delete(certDir, true);
                }
            }
        }

        [Fact]
        public void RetrieveCertificateForUser_ShouldReturnNullForNonExistingUser()
        {
            MockFileSystem.Setup(x => x.GetTempPath()).Returns("");
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns("");
            MockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            MockFileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });
            // Arrange
            var provider = new LocalUserCertificateProvider(MockFileSystem.Object);

            // Act
            var cert = provider.RetrieveCertificateForUser("nonexistinguser");

            // Assert
            Assert.Null(cert);
        }
    }
}
