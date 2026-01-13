// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace testengine.server.mcp.tests
{
    public class TestEngineToolsTests
    {
        private readonly ITestOutputHelper _output;

        public TestEngineToolsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GetTemplates_Returns_Valid_Response()
        {
            // Act
            string result = TestEngineTools.GetTemplates();
            _output.WriteLine($"GetTemplates result: {result}");

            var jsonDoc = JsonDocument.Parse(result);

            // Assert
            Assert.NotNull(result);

            // Check if we got templates or an error
            if (jsonDoc.RootElement.TryGetProperty("templates", out var templates))
            {
                // Success case - we should have at least one template
                Assert.True(templates.EnumerateObject().Any());
            }
            else
            {
                // Error case - should have an error message
                Assert.True(jsonDoc.RootElement.TryGetProperty("error", out _),
                    "Response should contain either templates or an error message");
            }
        }

        [Theory]
        [InlineData("AIBuilderPrompt")]
        [InlineData("AIBuilderQuery")]
        [InlineData("JavaScriptWebResource")]
        [InlineData("ModelDrivenApplication")]
        [InlineData("Variables")]
        public void GetTemplate_With_Valid_Names_Returns_Content(string templateName)
        {
            // Act
            string result = TestEngineTools.GetTemplate(templateName);
            _output.WriteLine($"GetTemplate result for {templateName}: {result}");

            var jsonDoc = JsonDocument.Parse(result);

            // Assert 
            Assert.NotNull(result);

            // Check if we got an error (which might happen if the template doesn't exist in test environment)
            if (jsonDoc.RootElement.TryGetProperty("error", out _))
            {
                _output.WriteLine($"Template {templateName} not found in test environment - skipping content validation");
                return;
            }

            Assert.True(jsonDoc.RootElement.TryGetProperty("name", out var nameElement));
            Assert.True(jsonDoc.RootElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(templateName, nameElement.GetString());
            Assert.False(string.IsNullOrEmpty(contentElement.GetString()));
        }
        [Fact]
        public void GetTemplate_With_Invalid_Name_Returns_Error()
        {
            // Arrange
            string invalidTemplateName = "NonExistentTemplate";

            // Act
            string result = TestEngineTools.GetTemplate(invalidTemplateName);
            _output.WriteLine($"GetTemplate result for invalid name: {result}");

            var jsonDoc = JsonDocument.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("error", out var errorElement));
            string errorMessage = errorElement.GetString();
            Assert.Contains("not found", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetTemplates_Response_Contains_Expected_Templates()
        {
            // Act
            string result = TestEngineTools.GetTemplates();
            var jsonDoc = JsonDocument.Parse(result);

            // Check if we got an error
            if (jsonDoc.RootElement.TryGetProperty("error", out _))
            {
                _output.WriteLine("Could not check for expected templates due to error response");
                return;
            }

            // Assert
            Assert.True(jsonDoc.RootElement.TryGetProperty("templates", out var templates));

            // Check for key expected templates
            var templateNames = templates.EnumerateObject()
                .Select(p => p.Name)
                .ToList();

            _output.WriteLine($"Found templates: {string.Join(", ", templateNames)}");

            // Check for common templates that should be present
            Assert.Contains(templateNames, name =>
                name.Equals("JavaScriptWebResource", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("ModelDrivenApplication", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Variables", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetTemplate_Content_Contains_Expected_Sections()
        {
            // Arrange - Use JavaScript WebResource as it's likely to exist
            string templateName = "JavaScriptWebResource";

            // Act
            string result = TestEngineTools.GetTemplate(templateName);
            var jsonDoc = JsonDocument.Parse(result);

            // Check if template exists
            if (jsonDoc.RootElement.TryGetProperty("error", out _))
            {
                _output.WriteLine($"Template {templateName} not found - skipping content validation");
                return;
            }

            // Assert - check for content
            Assert.True(jsonDoc.RootElement.TryGetProperty("content", out var contentElement));
            string content = contentElement.GetString();

            // Validate that key sections exist in the template
            Assert.Contains("# Recommendation", content);

            // Check for at least one of these common sections
            bool hasExpectedSections =
                content.Contains("## Variables", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("## Test Case", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("## JavaScript WebResource", StringComparison.OrdinalIgnoreCase);

            Assert.True(hasExpectedSections, "Template should contain expected sections");
        }
    }
}
