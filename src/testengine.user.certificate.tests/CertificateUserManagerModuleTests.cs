using System.Net;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Users;
using Moq;
using Moq.Protected;
using testengine.auth;
using testengine.user.environment;

namespace testengine.user.environment.tests
{
    public class CertificateUserManagerModuleTests: IDisposable
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
        private X509Certificate2 MockCert;

        //adding this to reset the behavior after each test case for the static function
        private readonly Func<HttpClientHandler> GetHttpClientHandler = CertificateUserManagerModule.GetHttpClientHandler;
        private readonly Func<HttpClientHandler, HttpClient> GetHttpClient = CertificateUserManagerModule.GetHttpClient;

        //adding this IDispose function for tear down that runs after each test and resets the GetHttpClientHandlerMock since it is static and can carry over between testcases
        public void Dispose()
        {
            // Reset static Func to its original value after each test
            CertificateUserManagerModule.GetHttpClientHandler = GetHttpClientHandler;
            CertificateUserManagerModule.GetHttpClient = GetHttpClient;
        }

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
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
                MockCert = new X509Certificate2(certificate.Export(X509ContentType.Pfx));
            }
        }

        [Fact]
        public async Task LoginAsUserSuccessTest()
        {
            var userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email",
                CertificateSubjectKey = "user1CertificateSubject"
            };

            var email = "someone@example.com";
            var certSubject = "CN=test";

            MockLogger = new Mock<ILogger>();

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.CertificateSubjectKey)).Returns(certSubject);
            MockTestInfraFunctions.Setup(x => x.GoToUrlAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            var keyboard = new Mock<IKeyboard>(MockBehavior.Strict);

            var mockEmailLocatorObject = new Mock<ILocator>();


            // Email Address
            MockPage.Setup(x => x.Locator(CertificateUserManagerModule.EmailSelector, null)).Returns(mockEmailLocatorObject.Object);
            mockEmailLocatorObject.Setup(x => x.PressSequentiallyAsync(email, It.IsAny<LocatorPressSequentiallyOptions>())).Returns(Task.CompletedTask);
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

            MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns(MockCert);
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
        public async Task LoginUserAsyncThrowsOnInvalidPersonaTest(string? persona)
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
        public async Task LoginUserAsyncThrowsOnInvalidUserConfigTest(string? emailKey)
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
        [InlineData(null, "", "set", "User email cannot be null. Please check if the environment variable is set properly.")]
        [InlineData("", "", "set", "User email cannot be null. Please check if the environment variable is set properly.")]
        [InlineData("someone@example.com", "CN=test", "setProvider", "Certificate provider cannot be null. Please ensure certificate provider for user.")]
        [InlineData("someone@example.com", "CN=test", "setCert", "Certificate cannot be null. Please ensure certificate for user.")]
        [InlineData("someone@example.com", null, "set", "User certificate subject name cannot be null. Please check if the environment variable is set properly.")]
        [InlineData("someone@example.com", "", "set", "User certificate subject name cannot be null. Please check if the environment variable is set properly.")]
        public async Task LoginUserAsyncThrowsOnInvalidEnviromentVariablesTest(string? email, string? certname, string setAsNull, string message)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = "user1Email",
                CertificateSubjectKey = "user1CertificateSubject"
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.EmailKey)).Returns(email);
            MockEnvironmentVariable.Setup(x => x.GetVariable(userConfiguration.CertificateSubjectKey)).Returns(certname);

            if (setAsNull == "setProvider")
            {
                MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns(MockCert);
                MockUserManagerLogin.Setup(x => x.UserCertificateProvider).Returns((IUserCertificateProvider)null);
            }
            else if(setAsNull == "setCert")
            {
                MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns((X509Certificate2)null);
                MockUserManagerLogin.Setup(x => x.UserCertificateProvider).Returns(MockUserCertificateProvider.Object);
            }
            else 
            {
                MockUserCertificateProvider.Setup(x => x.RetrieveCertificateForUser(It.IsAny<string>())).Returns(MockCert);
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
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(certname))
            {
                LoggingTestHelper.VerifyLogging(MockLogger, message, LogLevel.Error, Times.Once());
            }
        }

        [Fact]
        public async Task GetCertAuthGlob_ReturnsCorrectUrl()
        {
            // Arrange
            var userManager = new CertificateUserManagerModule();
            string endpoint = "example.com";
            string expected = "https://*certauth.example.com/**";

            // Act
            string result = await userManager.GetCertAuthGlob(endpoint);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task HandleRequest_PostMethod_Success()
        {
            // Arrange
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Method).Returns("POST");
            mockRequest.Setup(r => r.Url).Returns("https://example.com");
            mockRequest.Setup(r => r.PostData).Returns("postData");
            mockRequest.Setup(r => r.Headers).Returns(new Dictionary<string, string>());
            var mockRoute = new Mock<IRoute>();
            mockRoute.Setup(r => r.Request).Returns(mockRequest.Object);

            var handlerMock = new Mock<HttpClientHandler>();
            handlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            CertificateUserManagerModule.GetHttpClientHandler = () => handlerMock.Object;
            CertificateUserManagerModule.GetHttpClient = handler => new HttpClient(handler);
            var handler = new CertificateUserManagerModule();

            // Mock the DoCertAuthPostAsync method
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("response content")
            };

            // Act
            await handler.HandleRequest(mockRoute.Object, MockCert, MockLogger.Object);

            // Assert
            mockRoute.Verify(r => r.FulfillAsync(It.IsAny<RouteFulfillOptions>()), Times.Once);
        }

        [Fact]
        public async Task HandleRequest_PostMethod_Failure()
        {
            // Arrange
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(r => r.Method).Returns("POST");
            mockRequest.Setup(r => r.Url).Returns("https://example.com");
            mockRequest.Setup(r => r.PostData).Returns("postData");
            mockRequest.Setup(r => r.Headers).Returns(new Dictionary<string, string>());
            var mockRoute = new Mock<IRoute>();
            mockRoute.Setup(r => r.Request).Returns(mockRequest.Object);
            var loggerMock = new Mock<ILogger>();

            var handlerMock = new Mock<HttpClientHandler>();
            handlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));
            CertificateUserManagerModule.GetHttpClientHandler = () => handlerMock.Object;
            CertificateUserManagerModule.GetHttpClient = handler => new HttpClient(handler);
            
            var handler = new CertificateUserManagerModule();

            // Act
            await handler.HandleRequest(mockRoute.Object, MockCert, loggerMock.Object);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true), // It.IsAnyType is used to match any state
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), // Function to format the log message
            Times.AtLeastOnce);
            mockRoute.Verify(r => r.AbortAsync("failed"), Times.Once);
        }

        [Fact]
        public async Task HandleRequest_NonPostMethod()
        {
            // Arrange
            var mockRoute = new Mock<IRoute>();
            var mockRequest = new Mock<IRequest>();
            mockRequest.Setup(x => x.Method).Returns("GET");
            mockRoute.Setup(r => r.Request).Returns(mockRequest.Object);

            var handler = new CertificateUserManagerModule();

            // Act
            await handler.HandleRequest(mockRoute.Object, MockCert, MockLogger.Object);

            // Assert
            mockRoute.Verify(r => r.ContinueAsync(null), Times.Once);
        }

        [Fact]
        public async Task DoCertAuthPostAsync_SuccessfulResponse_ReturnsResponse()
        {
            // Arrange
            var request = new Mock<IRequest>();
            request.Setup(r => r.Url).Returns("https://example.com");
            request.Setup(r => r.PostData).Returns("postData");
            request.Setup(r => r.Headers).Returns(new Dictionary<string, string>());


            var handlerMock = new Mock<HttpClientHandler>();
            handlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            CertificateUserManagerModule.GetHttpClientHandler = () => handlerMock.Object;
            CertificateUserManagerModule.GetHttpClient = handler => new HttpClient(handler);

            var module = new CertificateUserManagerModule();

            // Act
            var response = await module.DoCertAuthPostAsync(request.Object, MockCert, MockLogger.Object);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DoCertAuthPostAsync_UnsuccessfulResponse_ThrowsHttpRequestException()
        {
            // Arrange
            var request = new Mock<IRequest>();
            request.Setup(r => r.Url).Returns("https://example.com");
            request.Setup(r => r.PostData).Returns("postData");
            request.Setup(r => r.Headers).Returns(new Dictionary<string, string>());
            var loggerMock = new Mock<ILogger>();
            var handlerMock = new Mock<HttpClientHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            CertificateUserManagerModule.GetHttpClientHandler = () => handlerMock.Object;
            CertificateUserManagerModule.GetHttpClient = handler => new HttpClient(handler);

            var module = new CertificateUserManagerModule();

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await module.DoCertAuthPostAsync(request.Object, MockCert, loggerMock.Object));
        }

        [Fact]
        public async Task DoCertAuthPostAsync_ExceptionThrown_LogsErrorAndThrows()
        {
            // Arrange
            var request = new Mock<IRequest>();
            request.Setup(r => r.Url).Returns("https://example.com");
            request.Setup(r => r.PostData).Returns("postData");
            request.Setup(r => r.Headers).Returns(new Dictionary<string, string>());

            var handlerMock = new Mock<HttpClientHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Request failed"));

            CertificateUserManagerModule.GetHttpClientHandler = () => handlerMock.Object;
            CertificateUserManagerModule.GetHttpClient = handler => new HttpClient(handler);
            var loggerMock = new Mock<ILogger>();
            var module = new CertificateUserManagerModule();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await module.DoCertAuthPostAsync(request.Object, MockCert, loggerMock.Object));

            // Verify logging
            loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true), // It.IsAnyType is used to match any state
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), // Function to format the log message
            Times.Once);
        }

        [Fact]
        public async Task ClickStaySignedIn_ShouldClickStaySignedIn_WhenDialogIsPresent()
        {
            MockLogger = new Mock<ILogger>();
            var MockSelectorOptions = new Mock<PageWaitForSelectorOptions>(MockBehavior.Strict);
            // Arrange
            MockPage.Setup(p => p.WaitForSelectorAsync(It.IsAny<string>(), It.IsAny<PageWaitForSelectorOptions>()))
                     .ReturnsAsync(new Mock<IElementHandle>().Object);
            MockPage.Setup(p => p.ClickAsync(It.IsAny<string>(), null))
                     .Returns(Task.CompletedTask);
            MockPage.Setup(p => p.WaitForURLAsync(It.IsAny<string>(), null))
                     .Returns(Task.CompletedTask);

            var handler = new CertificateUserManagerModule();
            handler.Page = MockPage.Object;
            // Act
            await handler.ClickStaySignedIn("https://example.com", MockLogger.Object);

            // Assert
            MockLogger.Verify(logger => logger.Log(LogLevel.Debug, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Was asked to 'stay signed in'.")),
                null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            MockPage.Verify(p => p.ClickAsync(It.IsAny<string>(), null), Times.Once);
        }

        [Fact]
        public async Task ClickStaySignedIn_ShouldLogAndContinue_WhenNoStaySignedInDialogAndNoCertError()
        {
            MockLogger = new Mock<ILogger>();
            // Arrange
            MockPage.SetupSequence(p => p.WaitForSelectorAsync(It.IsAny<string>(), It.IsAny<PageWaitForSelectorOptions>()))
                     .ThrowsAsync(new Exception()) // Simulate no 'Stay signed in?' dialog
                     .ThrowsAsync(new Exception()); // Simulate no certificate error
            MockPage.Setup(p => p.WaitForURLAsync(It.IsAny<string>(), null))
                     .Returns(Task.CompletedTask);

            var handler = new CertificateUserManagerModule();
            handler.Page = MockPage.Object;
            // Act
            await handler.ClickStaySignedIn("https://example.com", MockLogger.Object);

            // Assert
            MockLogger.Verify(logger => logger.Log(LogLevel.Debug, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Did not encounter an invalid certificate error.")),
                null, It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ClickStaySignedIn_ShouldThrowUserInputException_WhenCertErrorOccurs()
        {
            MockLogger = new Mock<ILogger>();
            // Arrange
            MockPage.SetupSequence(p => p.WaitForSelectorAsync(It.IsAny<string>(), It.IsAny<PageWaitForSelectorOptions>()))
                     .ThrowsAsync(new Exception()) // Simulate no 'Stay signed in?' dialog
                     .ReturnsAsync(new Mock<IElementHandle>().Object); // Simulate certificate error detected

            var handler = new CertificateUserManagerModule();
            handler.Page = MockPage.Object;
            // Act & Assert
            await Assert.ThrowsAsync<UserInputException>(() => handler.ClickStaySignedIn("http://desired.url", MockLogger.Object));
        }

        [Fact]
        public async Task ClickStaySignedIn_ShouldWaitForUrlAfterActions()
        {
            MockLogger = new Mock<ILogger>();
            // Arrange
            MockPage.Setup(p => p.WaitForSelectorAsync(It.IsAny<string>(), It.IsAny<PageWaitForSelectorOptions>()))
                     .ThrowsAsync(new Exception()); // Simulate no 'Stay signed in?' dialog and no cert error
            MockPage.Setup(p => p.WaitForURLAsync(It.IsAny<string>(), null))
                     .Returns(Task.CompletedTask);

            var handler = new CertificateUserManagerModule();
            handler.Page = MockPage.Object;
            // Act
            await handler.ClickStaySignedIn("http://desired.url", MockLogger.Object);

            // Assert
            MockPage.Verify(p => p.WaitForURLAsync("http://desired.url", null), Times.Once);
        }
    }
}
