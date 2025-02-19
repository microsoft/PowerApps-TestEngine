// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using Xunit;

namespace testengine.user.storagestate.tests
{
    public class DataverseStorageStateUserManagerModuleTests : IDisposable
    {
        const string CERTIFICATE_NAME = "CN=SelfSignedCert";

        private Mock<IBrowserContext> MockBrowserState;
        private Mock<ITestInfraFunctions> MockTestInfraFunctions;
        private Mock<ITestState> MockTestState;
        private Mock<ISingleTestInstanceState> MockSingleTestInstanceState;
        private Mock<IEnvironmentVariable> MockEnvironmentVariable;
        private TestSuiteDefinition TestSuiteDefinition;
        private Mock<ILogger> MockLogger;
        private Mock<IBrowserContext> MockBrowserContext;
        private Mock<IPage> MockPage;
        private Mock<IElementHandle> MockElementHandle;
        private Mock<IFileSystem> MockFileSystem;
        private Mock<IUserManagerLogin> MockUserManagerLogin;
        private Mock<IXmlRepository> MockXmlRepository;
        private Mock<IOrganizationService> MockOrganizationService;
        private Mock<IUserCertificateProvider> MockUserCertificateProvider;
        private string testFile = String.Empty;

        public DataverseStorageStateUserManagerModuleTests()
        {
            MockBrowserState = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            TestSuiteDefinition = new TestSuiteDefinition();
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockUserManagerLogin = new Mock<IUserManagerLogin>(MockBehavior.Strict);
            MockXmlRepository = new Mock<IXmlRepository>(MockBehavior.Strict);
            MockOrganizationService = new Mock<IOrganizationService>(MockBehavior.Strict);
            MockUserCertificateProvider = new Mock<IUserCertificateProvider>(MockBehavior.Strict);
            testFile = Path.Combine(Path.GetTempPath(), "test.json");
        }

        public void Dispose()
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }

        [Fact]
        public void TestGenerateSelfSignedCertificate()
        {
            // Arrange

            // Act
            var certificate = GenerateSelfSignedCertificate(CERTIFICATE_NAME);

            // Assert
            Assert.NotNull(certificate);
            Assert.Equal(CERTIFICATE_NAME, certificate.Subject);
        }

        [Fact]
        public void InMemoryProtectAndUnprotect()
        {
            // Arrange
            const string DATA = "test";


            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);

            TestSuiteDefinition testSuiteDefinition = new TestSuiteDefinition();
            testSuiteDefinition.Persona = "user1";

            var serviceCollection = new ServiceCollection();

            var repository = new TestXmlRepository();

            serviceCollection.AddSingleton<IOrganizationService>(MockOrganizationService.Object);
            serviceCollection.AddSingleton<ILogger>(MockLogger.Object);

            var dataverse = new DataverseStorageStateUserManagerModule();
            dataverse.Settings.Add("Environment", MockEnvironmentVariable.Object);
            dataverse.Settings.Add("State", MockTestState.Object);
            dataverse.Settings.Add("TestInstance", MockSingleTestInstanceState.Object);
            dataverse.Settings.Add("Cert", MockUserCertificateProvider.Object);
            dataverse.Settings.Add("Client", MockOrganizationService.Object);

            MockTestState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration("user1")).Returns(new UserConfiguration() { EmailKey = "user1Email" });

            MockEnvironmentVariable.Setup(x => x.GetVariable("DataverseKey")).Returns("TEST");
            MockEnvironmentVariable.Setup(x => x.GetVariable("user1Email")).Returns("contoso.onmicrosoft.com");
            MockEnvironmentVariable.Setup(x => x.GetVariable("DataProtectionUrl")).Returns("https://contoso.crm.dynamics.com");
            MockEnvironmentVariable.Setup(x => x.GetVariable("DataProtectionCertificateName")).Returns(CERTIFICATE_NAME);
            MockFileSystem.Setup(x => x.ReadAllText(testFile)).Returns(DATA);
            MockFileSystem.Setup(x => x.Delete(testFile));

            MockOrganizationService.Setup(x => x.Create(It.IsAny<Entity>()));

            MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(CERTIFICATE_NAME))
                .Returns(GenerateSelfSignedCertificate(CERTIFICATE_NAME));

            var results = new EntityCollection();

            MockOrganizationService.Setup(x => x.Create(It.IsAny<Entity>()))
                .Callback((Entity e) =>
                {
                    e.Id = Guid.NewGuid();
                    results.Entities.Add(e);
                })
                .Returns(() => results.Entities.First().Id);

            MockOrganizationService.Setup(x => x.RetrieveMultiple(It.IsAny<QueryBase>())).Returns(results);

            dataverse.SetupState(repository);

            // Act
            dataverse.Protect(MockFileSystem.Object, testFile);

            var load = (Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string>)dataverse.Settings[DataverseStorageStateUserManagerModule.LOAD_SETTINGS];

            string unprotected = load.DynamicInvoke(new object[] { MockEnvironmentVariable.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object }) as string;

            // Assert
            Assert.Equal(DATA, unprotected);

        }

        private static X509Certificate2 GenerateSelfSignedCertificate(string name)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    new X500DistinguishedName(name),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                var certificate = request.CreateSelfSigned(
                    DateTimeOffset.Now.AddMinutes(-1),
                    DateTimeOffset.Now.AddYears(1));

                return certificate;
            }
        }
    }


    public class TestXmlRepository : IXmlRepository
    {
        public List<XElement> Elements = new List<XElement>();

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return Elements.AsReadOnly();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            Elements.Add(element);
        }
    }
}
