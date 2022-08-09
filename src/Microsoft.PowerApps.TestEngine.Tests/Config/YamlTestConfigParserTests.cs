﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Config
{
    public class YamlTestConfigParserTests
    {
        [Fact]
        public void YamlTestConfigParserPaeseTestPlanTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
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
    engineLoggingLevel: Debug

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
            Assert.Equal("Debug", testPlan.TestSettings?.EngineLoggingLevel.ToString());
            Assert.Single(testPlan.EnvironmentVariables?.Users);
            Assert.Equal("User1", testPlan.EnvironmentVariables?.Users[0].PersonaName);
            Assert.Equal("user1Email", testPlan.EnvironmentVariables?.Users[0].EmailKey);
            Assert.Equal("user1Password", testPlan.EnvironmentVariables?.Users[0].PasswordKey);

        }

        [Fact]
        public void YamlTestConfigParserParseEnvironmentVariablesTest()
        {
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
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
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
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

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void YamlTestConfigParserThrowsOnNullArguments(string? filePath)
        {
            Mock<Microsoft.Extensions.Logging.ILogger> MockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>(MockBehavior.Loose);
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            Assert.Throws<ArgumentNullException>(() => parser.ParseTestConfig<TestPlanDefinition>(filePath));
        }
    }
}
