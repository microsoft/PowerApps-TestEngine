// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using System;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Config
{
    public class YamlTestConfigParserTests
    {
        [Fact]
        public void YamlTestConfigParserTest()
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);

            var yamlFile = @"test:
  name: Button Clicker
  description: Verifies that counter increments when the button is clicked
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

environmentVariables:
    users:
        - personaName: User1
          emailKey: user1Email
          passwordKey: user1Password";

            var filePath = "testplan.fx.yaml";
            mockFileSystem.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns(yamlFile);
            var testPlan = parser.ParseTestConfig(filePath);
            Assert.NotNull(testPlan);
            Assert.Equal("Button Clicker", testPlan.Test?.Name);
            Assert.Equal("Verifies that counter increments when the button is clicked", testPlan.Test?.Description);
            Assert.Equal("User1", testPlan.Test?.Persona);
            Assert.Equal("new_buttonclicker_0a877", testPlan.Test?.AppLogicalName);
            Assert.Equal("https://unitedstates-002.azure-apim.net/invoke", testPlan.Test?.NetworkRequestMocks?[0].RequestURL);
            Assert.Equal("POST", testPlan.Test?.NetworkRequestMocks?[0].Method);
            Assert.Equal("PATCH", testPlan.Test?.NetworkRequestMocks?[0].Headers?["x-ms-request-method"]);
            Assert.Equal("/items/4", testPlan.Test?.NetworkRequestMocks?[0].Headers?["x-ms-request-url"]);
            Assert.Equal("/myFakePayload.json", testPlan.Test?.NetworkRequestMocks?[0].RequestBodyFile);
            Assert.Equal("/myFakeBing.json", testPlan.Test?.NetworkRequestMocks?[0].ResponseDataFile);
            Assert.False(string.IsNullOrEmpty(testPlan.Test?.TestSteps));
            Assert.True(testPlan.TestSettings?.RecordVideo);
            Assert.Equal(2, testPlan.TestSettings?.BrowserConfigurations?.Count);
            Assert.Equal("Chromium", testPlan.TestSettings?.BrowserConfigurations?[0].Browser);
            Assert.Equal("Firefox", testPlan.TestSettings?.BrowserConfigurations?[1].Browser);
            Assert.Single(testPlan.EnvironmentVariables?.Users);
            Assert.Equal("User1", testPlan.EnvironmentVariables?.Users[0].PersonaName);
            Assert.Equal("user1Email", testPlan.EnvironmentVariables?.Users[0].EmailKey);
            Assert.Equal("user1Password", testPlan.EnvironmentVariables?.Users[0].PasswordKey);

        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void YamlTestConfigParserThrowsOnNullArguments(string? filePath)
        {
            var mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            var parser = new YamlTestConfigParser(mockFileSystem.Object);
            Assert.Throws<ArgumentNullException>(() => parser.ParseTestConfig(filePath));
        }
    }
}
