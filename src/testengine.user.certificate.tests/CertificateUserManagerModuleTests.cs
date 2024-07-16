using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Moq;
using testengine.auth;
using testengine.user.environment;

namespace testengine.user.certificate.tests
{
    public class CertificateUserManagerModuleTests
    {
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
        private Mock<IUserCertificateProvider> MockUserCertificateProvider;

        public CertificateUserManagerModuleTests()
        {
            MockBrowserState = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockTestInfraFunctions = new Mock<ITestInfraFunctions>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockSingleTestInstanceState = new Mock<ISingleTestInstanceState>(MockBehavior.Strict);
            MockEnvironmentVariable = new Mock<IEnvironmentVariable>(MockBehavior.Strict);
            TestSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
            MockBrowserContext = new Mock<IBrowserContext>(MockBehavior.Strict);
            MockPage = new Mock<IPage>(MockBehavior.Strict);
            MockElementHandle = new Mock<IElementHandle>(MockBehavior.Strict);
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            MockUserManagerLogin = new Mock<IUserManagerLogin>(MockBehavior.Strict);
            MockUserCertificateProvider = new Mock<IUserCertificateProvider>(MockBehavior.Strict);
        }

        [Fact]
        public async Task LoginAsUserSuccessTest()
        {
            var userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email"
            };

            var email = "someone@example.com";

            MockLogger = new Mock<ILogger>();

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            var keyboard = new Mock<IKeyboard>(MockBehavior.Strict);

            // Email Address
            MockPage.Setup(x => x.Locator(CertificateUserManagerModule.EmailSelector, null)).Returns(new Mock<ILocator>().Object);
            MockPage.Setup(x => x.TypeAsync(CertificateUserManagerModule.EmailSelector, email, It.IsAny<PageTypeOptions>())).Returns(Task.CompletedTask);
            keyboard.Setup(x => x.PressAsync("Tab", It.IsAny<KeyboardPressOptions>()))
                    .Returns(Task.CompletedTask);
            MockPage.SetupGet(x => x.Keyboard).Returns(keyboard.Object);
            MockPage.Setup(x => x.ClickAsync(CertificateUserManagerModule.SubmitButtonSelector, null)).Returns(Task.CompletedTask);

            // Enter Password and keep me signed in
            MockPage.Setup(x => x.ClickAsync(CertificateUserManagerModule.SubmitButtonSelector, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync(CertificateUserManagerModule.StaySignedInSelector, null)).Returns(Task.CompletedTask);
            MockPage.Setup(x => x.ClickAsync(CertificateUserManagerModule.KeepMeSignedInNoSelector, null)).Returns(Task.CompletedTask);

            // Now wait for the requested URL assuming login now complete
            MockPage.Setup(x => x.WaitForURLAsync("*", null)).Returns(Task.CompletedTask);

            X509Certificate2 mockCert;
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

                mockCert = new X509Certificate2(certificate.Export(X509ContentType.Pfx));
            }

            MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns(mockCert);
            MockUserManagerLogin.Setup(x => x.UserCertificateProvider).Returns(MockUserCertificateProvider.Object);

            MockPage.Setup(x => x.Url).Returns("https://contoso.powerappsportals.com");
            MockPage.Setup(x => x.RouteAsync(It.IsAny<string>(), It.IsAny<Func<IRoute, Task>>(), null)).Returns(Task.CompletedTask);

            MockPage.Setup(x => x.WaitForSelectorAsync("[id=\"KmsiCheckboxField\"]", It.IsAny<PageWaitForSelectorOptions>())).Returns(Task.FromResult(MockElementHandle.Object));
            // Simulate Click to stay signed in 
            MockPage.Setup(x => x.ClickAsync("[id=\"idBtn_Back\"]", null)).Returns(Task.CompletedTask);
            // Wait until login is complete and redirect to desired page
            MockPage.Setup(x => x.WaitForURLAsync("*", null)).Returns(Task.CompletedTask);


            var mockLocatorObject = new Mock<ILocator>();
            mockLocatorObject.Setup(locator => locator.IsVisibleAsync(It.IsAny<LocatorIsVisibleOptions>())).ReturnsAsync(true);
            MockPage.Setup(x => x.GetByRole(It.IsAny<AriaRole>(), It.IsAny<PageGetByRoleOptions>())).Returns(mockLocatorObject.Object);
            mockLocatorObject.Setup(re => re.Or(mockLocatorObject.Object)).Returns(mockLocatorObject.Object);

            var userManager = new CertificateUserManagerModule();
            
            var responseReceivedField = typeof(CertificateUserManagerModule).GetField("responseReceived", BindingFlags.NonPublic | BindingFlags.Instance);
            var mockResponseReceived = new TaskCompletionSource<bool>();
            mockResponseReceived.SetResult(true);
            responseReceivedField.SetValue(userManager, mockResponseReceived);

            userManager.Page = MockPage.Object;
            

            await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            MockSingleTestInstanceState.Verify(x => x.GetTestSuiteDefinition(), Times.Once());
            MockTestState.Verify(x => x.GetUserConfiguration(userConfiguration.PersonaName), Times.Once());
            MockEnvironmentVariable.Verify(x => x.GetVariable(userConfiguration.EmailKey), Times.Once());
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullTestDefinitionTest()
        {
            TestSuiteDefinition testSuiteDefinition = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new CertificateUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task LoginUserAsyncThrowsOnInvalidPersonaTest(string persona)
        {
            var testSuiteDefinition = new TestSuiteDefinition()
            {
                TestSuiteName = "Test1",
                TestSuiteDescription = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = persona,
                TestCases = new List<TestCase>()
                {
                    new TestCase
                    {
                        TestCaseName = "Test Case Name",
                        TestCaseDescription = "Test Case Description",
                        TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
                    }
                }
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(testSuiteDefinition);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new CertificateUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Fact]
        public async Task LoginUserAsyncThrowsOnNullUserConfigTest()
        {
            UserConfiguration userConfiguration = null;

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new CertificateUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task LoginUserAsyncThrowsOnInvalidUserConfigTest(string emailKey)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new CertificateUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Theory]
        [InlineData(null, "set", "User email cannot be null. Please check if the environment variable is set properly.")]
        [InlineData("", "set", "User email cannot be null. Please check if the environment variable is set properly.")]
        [InlineData("someone@example.com", "setProvider", "Certificate provider cannot be null. Please ensure certificate provider for user.")]
        [InlineData("someone@example.com", "setCert", "Certificate cannot be null. Please ensure certificate for user.")]
        public async Task LoginUserAsyncThrowsOnInvalidEnviromentVariablesTest(string email, string setAsNull, string message)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email"
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            X509Certificate2 mockCert;
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

                mockCert = new X509Certificate2(certificate.Export(X509ContentType.Pfx));
            }

            if (setAsNull == "setProvider")
            {
                MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns(mockCert);
                MockUserManagerLogin.Setup(x => x.UserCertificateProvider).Returns((IUserCertificateProvider)null);
            }
            else if(setAsNull == "setCert")
            {
                MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns((X509Certificate2)null);
                MockUserManagerLogin.Setup(x => x.UserCertificateProvider).Returns(MockUserCertificateProvider.Object);
            }
            else 
            {
                MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns(mockCert);
                MockUserManagerLogin.Setup(x => x.UserCertificateProvider).Returns(MockUserCertificateProvider.Object);
            }
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            LoggingTestHelper.SetupMock(MockLogger);
            MockBrowserContext.SetupGet(x => x.Pages).Returns(new List<IPage> { MockPage.Object });

            var userManager = new CertificateUserManagerModule();
            userManager.Page = MockPage.Object;

            var ex = await Assert.ThrowsAsync<UserInputException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));

            Assert.Equal(UserInputException.ErrorMapping.UserInputExceptionLoginCredential.ToString(), ex.Message);
            if (String.IsNullOrEmpty(email))
            {
                LoggingTestHelper.VerifyLogging(MockLogger, message, LogLevel.Error, Times.Once());
            }
        }
    }
}
