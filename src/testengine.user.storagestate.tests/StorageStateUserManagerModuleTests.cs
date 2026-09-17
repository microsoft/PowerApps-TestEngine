// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerApps.TestEngine.Users;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace testengine.user.storagestate.tests
{
    public class StorageStateUserManagerModuleTests
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

        public StorageStateUserManagerModuleTests()
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
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("user1Email", "")]
        [InlineData("user1Email", null)]
        public async Task LoginUserAsyncThrowsOnInvalidUserConfigTest(string? emailKey, string? emailValue)
        {
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            LoggingTestHelper.SetupMock(MockLogger);

            var userManager = new StorageStateUserManagerModule();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await userManager.LoginAsUserAsync("*",
                MockBrowserState.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object));
        }

        [Theory]
        [InlineData(null, "", false, "", "")]
        [InlineData("", "", false, "", "")]
        [InlineData("user1Email", "", false, "", "")]
        [InlineData("user1Email", null, false, "", "")]
        [InlineData("user1Email", "user1", false, "", "")]
        [InlineData("user1Email", "user1@example.com", false, "", "")]
        public async Task LoadState(string? emailKey, string? emailValue, bool exists, string content, string expectedState)
        {
            // Arrange
            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            LoggingTestHelper.SetupMock(MockLogger);

            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns("");
            MockFileSystem.Setup(x => x.Exists(".storage-state-user1")).Returns(exists);
            if (content != null)
            {
                MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns(String.Empty);
                MockFileSystem.Setup(x => x.FileExists(Path.Combine(".storage-state-user1", "state.json"))).Returns(true);
                MockFileSystem.Setup(x => x.ReadAllText(Path.Combine(".storage-state-user1", "state.json"))).Returns(content);
            }

            var userManager = new StorageStateUserManagerModule();

            // Act
            var loadState = userManager.Settings["LoadState"] as Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string>;
            var state = loadState.DynamicInvoke(MockEnvironmentVariable.Object, MockSingleTestInstanceState.Object, MockTestState.Object, MockFileSystem.Object);

            // Assert
            Assert.Equal(expectedState, state);
        }

        string[] errorSelectors = new string[] { ".ms-Dialog-title", "#ErrorTitle", ".NotificationTitle" };

        [Theory]
        [InlineData("user1Email", "user1@example.com", "https://example.com", "https://example.com", "https://example.com")]
        [InlineData("user1Email", "user1@example.com", "https://example.com", "https://example.com.mcas.ms", "https://example.com.mcas.ms")]
        public async Task ValidLogin(string emailKey, string emailValue, string desiredUrl, string pageUrl, string foundUrl)
        {
            // Arrange
            var userManager = new StorageStateUserManagerModule();

            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            userManager.Settings.Add("FileSystem", MockFileSystem.Object);
            userManager.Protect = (IFileSystem filesystem, string file) => { };
            userManager.Unprotect = (IFileSystem filesystem, string file) => { return "test"; };

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns("");
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns(String.Empty);
            MockFileSystem.Setup(x => x.Exists(".storage-state-user1")).Returns(true);
            MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.SetupGet(x => x.Url).Returns(pageUrl);
            MockPage.Setup(x => x.EvaluateAsync<string>(It.Is<string>(s => errorSelectors.Any(selector => s.Contains(selector))), null)).Returns(Task.FromResult(""));
            MockPage.Setup(x => x.EvaluateAsync<string>(It.Is<string>(s => !errorSelectors.Any(selector => s.Contains(selector))), null)).Returns(Task.FromResult("Idle"));
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestState.Setup(x => x.GetTimeout()).Returns(0);
            MockBrowserContext.Setup(x => x.StorageStateAsync(It.IsAny<BrowserContextStorageStateOptions>())).Returns(Task.FromResult(""));
            MockTestState.Setup(x => x.GetDomain()).Returns(String.Empty);
            MockTestState.Setup(x => x.SetDomain(foundUrl));

            // Act
            await userManager.LoginAsUserAsync(desiredUrl,
                MockBrowserContext.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            // Assert
        }

        [Theory]
        [InlineData("user1Email", "user1@example.com", "https://example.com", "https://example.com", "https://example.com", ".ms-Dialog-title", "Text", "Text")]
        [InlineData("user1Email", "user1@example.com", "https://example.com", "https://example.com", "https://example.com", "#ErrorTitle", "Text", "Text")]
        [InlineData("user1Email", "user1@example.com", "https://example.com", "https://example.com", "https://example.com", ".NotificationTitle", "Text", "Text")]
        public async Task ErrorLogin(string emailKey, string emailValue, string desiredUrl, string pageUrl, string foundUrl, string match, string response, string expectedError)
        {
            // Arrange
            var userManager = new StorageStateUserManagerModule();

            UserConfiguration userConfiguration = new UserConfiguration()
            {
                PersonaName = "User1",
                EmailKey = emailKey
            };

            userManager.Settings.Add("FileSystem", MockFileSystem.Object);

            MockSingleTestInstanceState.Setup(x => x.GetTestSuiteDefinition()).Returns(TestSuiteDefinition);
            MockTestState.Setup(x => x.GetUserConfiguration(It.IsAny<string>())).Returns(userConfiguration);
            MockSingleTestInstanceState.Setup(x => x.GetLogger()).Returns(MockLogger.Object);
            MockEnvironmentVariable.Setup(x => x.GetVariable(emailKey)).Returns(emailValue);
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns("");
            MockFileSystem.Setup(x => x.GetDefaultRootTestEngine()).Returns(String.Empty);
            MockFileSystem.Setup(x => x.Exists(".storage-state-user1")).Returns(true);
            MockBrowserContext.Setup(x => x.Pages).Returns(new List<IPage>() { MockPage.Object });
            MockPage.SetupGet(x => x.Url).Returns(pageUrl);
            MockPage.Setup(x => x.EvaluateAsync<string>(It.Is<string>(s => !errorSelectors.Any(selector => s.Contains(selector))), null)).Returns(Task.FromResult("Idle"));
            MockPage.Setup(x => x.EvaluateAsync<string>(It.Is<string>(s => s.Contains(match)), null)).Returns(Task.FromResult(response));
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestState.Setup(x => x.GetTimeout()).Returns(0);
            MockBrowserContext.Setup(x => x.StorageStateAsync(It.IsAny<BrowserContextStorageStateOptions>())).Returns(Task.FromResult(""));
            MockTestState.Setup(x => x.GetDomain()).Returns(String.Empty);
            MockTestState.Setup(x => x.SetDomain(foundUrl));

            // Act
            await userManager.LoginAsUserAsync(desiredUrl,
                MockBrowserContext.Object,
                MockTestState.Object,
                MockSingleTestInstanceState.Object,
                MockEnvironmentVariable.Object,
                MockUserManagerLogin.Object);

            // Assert
            Assert.Equal(expectedError, userManager.Settings["ErrorDialogTitle"]);
        }

        [Fact]
        public async Task ProtectandUnprotect()
        {
            // Windows Data Protection API is only supported on Windows. 
            if (PlatformHelper.IsWindows())
            {
                // Arrange
                var userManager = new StorageStateUserManagerModule();

                const string DATA = "sample";
                const string TEST_FILE = "test.json";

                string encryptedData = String.Empty;

                MockFileSystem.Setup(m => m.ReadAllText(TEST_FILE)).Returns(DATA);
                MockFileSystem.Setup(m => m.WriteTextToFile(TEST_FILE, It.IsAny<string>(), true))
                    .Callback((string filename, string encrypted, bool overwrite) => encryptedData = encrypted);

                var mockProtected = new Mock<IFileSystem>();
                mockProtected.Setup(m => m.ReadAllText(TEST_FILE)).Returns(() => encryptedData);

                // Act
                userManager.Protect(MockFileSystem.Object, TEST_FILE);
                Assert.False(string.IsNullOrEmpty(encryptedData));

                var unprotected = userManager.Unprotect(mockProtected.Object, TEST_FILE);

                // Assert
                Assert.Equal(DATA, unprotected);
            }
        }

        [Fact]
        public void Protect_Should_Delete_File_When_Encryption_Fails()
        {
            // Arrange
            var testFile = "test-state.json";
            var testContent = "{\"cookies\":[{\"name\":\"test\",\"value\":\"data\"}]}\"";

            MockFileSystem.Setup(f => f.ReadAllText(testFile)).Returns(testContent);
            MockFileSystem.Setup(f => f.WriteTextToFile(testFile, It.IsAny<string>(), true))
                .Throws(new System.Security.Cryptography.CryptographicException("Encryption failed"));
            MockFileSystem.Setup(f => f.Delete(testFile));

            var userManager = new StorageStateUserManagerModule();

            // Act & Assert
            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
                userManager.Protect(MockFileSystem.Object, testFile));

            // Verify file was deleted in finally block
            MockFileSystem.Verify(f => f.Delete(testFile), Times.Once,
                "File should be deleted when encryption fails");
        }

        [Fact]
        public void Protect_Should_Delete_File_When_ReadAllText_Fails()
        {
            // Arrange
            var testFile = "test-state.json";

            MockFileSystem.Setup(f => f.ReadAllText(testFile))
                .Throws(new IOException("Failed to read file"));
            MockFileSystem.Setup(f => f.Delete(testFile));

            var userManager = new StorageStateUserManagerModule();

            // Act & Assert
            Assert.Throws<IOException>(() =>
                userManager.Protect(MockFileSystem.Object, testFile));

            // Verify file was deleted in finally block
            MockFileSystem.Verify(f => f.Delete(testFile), Times.Once,
                "File should be deleted when read fails");
        }
    }
}
