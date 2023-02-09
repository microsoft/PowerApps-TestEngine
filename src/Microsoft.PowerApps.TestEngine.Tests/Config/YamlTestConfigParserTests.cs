// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Config
{
    public class YamlTestConfigParserTests
    {
        [Fact]
        public void YamlTestConfigParserParseTestPlanWithAppLogicalNameTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            var yamlFile = @"testSuite:
  testSuiteName: Button Clicker
  testSuiteDescription: Verifies that counter increments when the button is clicked
  persona: User1
  appLogicalName: new_buttonclicker_0a877
  networkRequestMocks:
    - requestURL: https://unitedstates-002.azure-apim.net/invoke
      method: POST
      headers:
        x-ms-request-method: PATCH
        x-ms-request-url: /items/4
      requestBodyFile: /myFakePayload.json
      responseDataFile: /myFakeBing.json

  testCases:
    - testCaseName: Case1
      testCaseDescription: Optional
      testSteps: |
        = Screenshot(""buttonclicker_loaded.png"");
          // Wait for the label to be set to 0
          //Wait(Label1.Text = ""0"");
          Wait(Label1, ""Text"", ""0"");
          // Click the button
          Select(Button1);
          Assert(Text(Label1.Text) = ""1"", ""Counter should be incremented to 1"");
          Screenshot(""buttonclicker_end.png"");

testSettings:
    recordVideo: true
    browserConfigurations:
        - browser: Chromium
        - browser: Firefox
    headless: false
    enablePowerFxOverlay: false

environmentVariables:
    users:
        - personaName: User1
          emailKey: user1Email
          passwordKey: user1Password";

            var filePath = "testplan.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(yamlFile);
            var testPlan = parser.ParseTestConfig<TestPlanDefinition>(filePath);
            Assert.NotNull(testPlan);
            Assert.Equal("Button Clicker", testPlan.TestSuite?.TestSuiteName);
            Assert.Equal("Verifies that counter increments when the button is clicked", testPlan.TestSuite?.TestSuiteDescription);
            Assert.Equal("User1", testPlan.TestSuite?.Persona);
            Assert.Equal("new_buttonclicker_0a877", testPlan.TestSuite?.AppLogicalName);
            Assert.Equal("https://unitedstates-002.azure-apim.net/invoke", testPlan.TestSuite?.NetworkRequestMocks?[0].RequestURL);
            Assert.Equal("POST", testPlan.TestSuite?.NetworkRequestMocks?[0].Method);
            Assert.Equal("PATCH", testPlan.TestSuite?.NetworkRequestMocks?[0].Headers?["x-ms-request-method"]);
            Assert.Equal("/items/4", testPlan.TestSuite?.NetworkRequestMocks?[0].Headers?["x-ms-request-url"]);
            Assert.Equal("/myFakePayload.json", testPlan.TestSuite?.NetworkRequestMocks?[0].RequestBodyFile);
            Assert.Equal("/myFakeBing.json", testPlan.TestSuite?.NetworkRequestMocks?[0].ResponseDataFile);
            Assert.True(testPlan.TestSuite?.TestCases?.Count > 0);
            Assert.Equal("Case1", testPlan.TestSuite?.TestCases[0].TestCaseName);
            Assert.Equal("Optional", testPlan.TestSuite?.TestCases[0].TestCaseDescription);
            Assert.False(string.IsNullOrEmpty(testPlan.TestSuite?.TestCases[0].TestSteps));
            Assert.True(testPlan.TestSettings?.RecordVideo);
            Assert.False(testPlan.TestSettings?.Headless);
            Assert.False(testPlan.TestSettings?.EnablePowerFxOverlay);
            Assert.Equal(2, testPlan.TestSettings?.BrowserConfigurations?.Count);
            Assert.Equal("Chromium", testPlan.TestSettings?.BrowserConfigurations?[0].Browser);
            Assert.Equal("Firefox", testPlan.TestSettings?.BrowserConfigurations?[1].Browser);
            Assert.Single(testPlan.EnvironmentVariables?.Users);
            Assert.Equal("User1", testPlan.EnvironmentVariables?.Users[0].PersonaName);
            Assert.Equal("user1Email", testPlan.EnvironmentVariables?.Users[0].EmailKey);
            Assert.Equal("user1Password", testPlan.EnvironmentVariables?.Users[0].PasswordKey);

        }

        [Fact]
        public void YamlTestConfigParserParseTestPlanWithAppIDTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            var yamlFile = $@"testSuite:
  testSuiteName: Button Clicker
  testSuiteDescription: Verifies that counter increments when the button is clicked
  persona: User1
  appId: 1253535
  networkRequestMocks:
    - requestURL: https://unitedstates-002.azure-apim.net/invoke
      method: POST
      headers:
        x-ms-request-method: PATCH
        x-ms-request-url: /items/4
      requestBodyFile: /myFakePayload.json
      responseDataFile: /myFakeBing.json

  testCases:
    - testCaseName: Case1
      testCaseDescription: Optional
      testSteps: |
        = Screenshot(""buttonclicker_loaded.png"");
          // Wait for the label to be set to 0
          //Wait(Label1.Text = ""0"");
          Wait(Label1, ""Text"", ""0"");
          // Click the button
          Select(Button1);
          Assert(Text(Label1.Text) = ""1"", ""Counter should be incremented to 1"");
          Screenshot(""buttonclicker_end.png"");

testSettings:
    recordVideo: true
    browserConfigurations:
        - browser: Chromium
        - browser: Firefox
    headless: false
    enablePowerFxOverlay: false

environmentVariables:
    users:
        - personaName: User1
          emailKey: user1Email
          passwordKey: user1Password";

            var filePath = "testplan.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(yamlFile);
            var testPlan = parser.ParseTestConfig<TestPlanDefinition>(filePath);
            Assert.NotNull(testPlan);
            Assert.Equal("Button Clicker", testPlan.TestSuite?.TestSuiteName);
            Assert.Equal("Verifies that counter increments when the button is clicked", testPlan.TestSuite?.TestSuiteDescription);
            Assert.Equal("User1", testPlan.TestSuite?.Persona);
            Assert.Equal("1253535", testPlan.TestSuite?.AppId);
            Assert.Equal("https://unitedstates-002.azure-apim.net/invoke", testPlan.TestSuite?.NetworkRequestMocks?[0].RequestURL);
            Assert.Equal("POST", testPlan.TestSuite?.NetworkRequestMocks?[0].Method);
            Assert.Equal("PATCH", testPlan.TestSuite?.NetworkRequestMocks?[0].Headers?["x-ms-request-method"]);
            Assert.Equal("/items/4", testPlan.TestSuite?.NetworkRequestMocks?[0].Headers?["x-ms-request-url"]);
            Assert.Equal("/myFakePayload.json", testPlan.TestSuite?.NetworkRequestMocks?[0].RequestBodyFile);
            Assert.Equal("/myFakeBing.json", testPlan.TestSuite?.NetworkRequestMocks?[0].ResponseDataFile);
            Assert.True(testPlan.TestSuite?.TestCases?.Count > 0);
            Assert.Equal("Case1", testPlan.TestSuite?.TestCases[0].TestCaseName);
            Assert.Equal("Optional", testPlan.TestSuite?.TestCases[0].TestCaseDescription);
            Assert.False(string.IsNullOrEmpty(testPlan.TestSuite?.TestCases[0].TestSteps));
            Assert.True(testPlan.TestSettings?.RecordVideo);
            Assert.False(testPlan.TestSettings?.Headless);
            Assert.False(testPlan.TestSettings?.EnablePowerFxOverlay);
            Assert.Equal(2, testPlan.TestSettings?.BrowserConfigurations?.Count);
            Assert.Equal("Chromium", testPlan.TestSettings?.BrowserConfigurations?[0].Browser);
            Assert.Equal("Firefox", testPlan.TestSettings?.BrowserConfigurations?[1].Browser);
            Assert.Single(testPlan.EnvironmentVariables?.Users);
            Assert.Equal("User1", testPlan.EnvironmentVariables?.Users[0].PersonaName);
            Assert.Equal("user1Email", testPlan.EnvironmentVariables?.Users[0].EmailKey);
            Assert.Equal("user1Password", testPlan.EnvironmentVariables?.Users[0].PasswordKey);

        }

        [Fact]
        public void YamlTestConfigParserParseTestPlanWithLocaleTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            var yamlFile = $@"testSuite:
  testSuiteName: Button Clicker
  testSuiteDescription: Verifies that counter increments when the button is clicked
  persona: User1
  appId: 1253535
  networkRequestMocks:
    - requestURL: https://unitedstates-002.azure-apim.net/invoke
      method: POST
      headers:
        x-ms-request-method: PATCH
        x-ms-request-url: /items/4
      requestBodyFile: /myFakePayload.json
      responseDataFile: /myFakeBing.json

  testCases:
    - testCaseName: Case1
      testCaseDescription: Optional
      testSteps: |
        = Screenshot(""buttonclicker_loaded.png"");;
          // Wait for the label to be set to 0
          //Wait(Label1.Text = ""0"");;
          Wait(Label1; ""Text""; ""0"");;
          // Click the button
          Select(Button1);;
          Assert(Text(Label1.Text) = ""1""; ""Counter should be incremented to 1"");;
          Screenshot(""buttonclicker_end.png"");;

testSettings:
    locale: ""de-DE""
    recordVideo: true
    browserConfigurations:
        - browser: Chromium
        - browser: Firefox
    headless: false
    enablePowerFxOverlay: false

environmentVariables:
    users:
        - personaName: User1
          emailKey: user1Email
          passwordKey: user1Password";

            var filePath = "testplan.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(yamlFile);
            var testPlan = parser.ParseTestConfig<TestPlanDefinition>(filePath);
            Assert.NotNull(testPlan);
            Assert.Equal("Button Clicker", testPlan.TestSuite?.TestSuiteName);
            Assert.Equal("Verifies that counter increments when the button is clicked", testPlan.TestSuite?.TestSuiteDescription);
            Assert.Equal("User1", testPlan.TestSuite?.Persona);
            Assert.Equal("1253535", testPlan.TestSuite?.AppId);
            Assert.Equal("https://unitedstates-002.azure-apim.net/invoke", testPlan.TestSuite?.NetworkRequestMocks?[0].RequestURL);
            Assert.Equal("POST", testPlan.TestSuite?.NetworkRequestMocks?[0].Method);
            Assert.Equal("PATCH", testPlan.TestSuite?.NetworkRequestMocks?[0].Headers?["x-ms-request-method"]);
            Assert.Equal("/items/4", testPlan.TestSuite?.NetworkRequestMocks?[0].Headers?["x-ms-request-url"]);
            Assert.Equal("/myFakePayload.json", testPlan.TestSuite?.NetworkRequestMocks?[0].RequestBodyFile);
            Assert.Equal("/myFakeBing.json", testPlan.TestSuite?.NetworkRequestMocks?[0].ResponseDataFile);
            Assert.True(testPlan.TestSuite?.TestCases?.Count > 0);
            Assert.Equal("Case1", testPlan.TestSuite?.TestCases[0].TestCaseName);
            Assert.Equal("Optional", testPlan.TestSuite?.TestCases[0].TestCaseDescription);
            Assert.False(string.IsNullOrEmpty(testPlan.TestSuite?.TestCases[0].TestSteps));
            Assert.True(testPlan.TestSettings?.RecordVideo);
            Assert.False(testPlan.TestSettings?.Headless);
            Assert.False(testPlan.TestSettings?.EnablePowerFxOverlay);
            Assert.Equal(2, testPlan.TestSettings?.BrowserConfigurations?.Count);
            Assert.Equal("Chromium", testPlan.TestSettings?.BrowserConfigurations?[0].Browser);
            Assert.Equal("Firefox", testPlan.TestSettings?.BrowserConfigurations?[1].Browser);
            Assert.Equal("de-DE", testPlan.TestSettings?.Locale);
            Assert.Single(testPlan.EnvironmentVariables?.Users);
            Assert.Equal("User1", testPlan.EnvironmentVariables?.Users[0].PersonaName);
            Assert.Equal("user1Email", testPlan.EnvironmentVariables?.Users[0].EmailKey);
            Assert.Equal("user1Password", testPlan.EnvironmentVariables?.Users[0].PasswordKey);

        }

        [Fact]
        public void YamlTestConfigParserParseEnvironmentVariablesTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);

            var environmentVariablesFile = @"users:
  - personaName: User1
    emailKey: user1Email
    passwordKey: user1Password";

            var filePath = "environmentVariables.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(environmentVariablesFile);
            var environmentVariables = parser.ParseTestConfig<EnvironmentVariables>(filePath);
            Assert.NotNull(environmentVariables);
            Assert.Single(environmentVariables.Users);
            Assert.Equal("User1", environmentVariables.Users[0].PersonaName);
            Assert.Equal("user1Email", environmentVariables.Users[0].EmailKey);
            Assert.Equal("user1Password", environmentVariables.Users[0].PasswordKey);
        }

        [Fact]
        public void YamlTestConfigParserParseTestSettingsTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);

            var testSettingsFile = @"recordVideo: true
browserConfigurations:
    - browser: Chromium
    - browser: Firefox
headless: false
enablePowerFxOverlay: false";

            var filePath = "testSettings.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(testSettingsFile);
            var testSettings = parser.ParseTestConfig<TestSettings>(filePath);
            Assert.NotNull(testSettings);
            Assert.True(testSettings.RecordVideo);
            Assert.False(testSettings.Headless);
            Assert.False(testSettings.EnablePowerFxOverlay);
            Assert.Equal(2, testSettings.BrowserConfigurations?.Count);
            Assert.Equal("Chromium", testSettings.BrowserConfigurations?[0].Browser);
            Assert.Equal("Firefox", testSettings.BrowserConfigurations?[1].Browser);
        }

        [Fact]
        public void YamlTestConfigParserParseTestSettingsWithLocaleTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);

            var testSettingsFile = @"recordVideo: true
locale: ""de-DE""
browserConfigurations:
    - browser: Chromium
    - browser: Firefox
headless: false
enablePowerFxOverlay: false";

            var filePath = "testSettings.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(testSettingsFile);
            var testSettings = parser.ParseTestConfig<TestSettings>(filePath);
            Assert.NotNull(testSettings);
            Assert.True(testSettings.RecordVideo);
            Assert.False(testSettings.Headless);
            Assert.False(testSettings.EnablePowerFxOverlay);
            Assert.Equal(2, testSettings.BrowserConfigurations?.Count);
            Assert.Equal("Chromium", testSettings.BrowserConfigurations?[0].Browser);
            Assert.Equal("Firefox", testSettings.BrowserConfigurations?[1].Browser);
            Assert.Equal("de-DE", testSettings.Locale);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void YamlTestConfigParserThrowsOnNullArguments(string filePath)
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            Assert.Throws<ArgumentNullException>(() => parser.ParseTestConfig<TestPlanDefinition>(filePath));
        }
    }
}
