// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    /// <summary>
    /// Tests for the URL parsing functionality in TestRunSummary
    /// </summary>
    public class TestRunUrlParsingTests
    {
        [Theory]
        [InlineData("https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account", "Model-driven App", "entitylist", "account")]
        [InlineData("https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=custompage", "Model-driven App", "Custom Page", "custompage")]
        [InlineData("https://contoso.crm4.dynamics.com/main.aspx?pagetype=entity&etn=contact", "Model-driven App", "entity", "contact")]
        [InlineData("https://apps.powerapps.com/play/e/default-tenant/a/1234abcd", "Canvas App", "Unknown", "Unknown")]
        [InlineData("https://make.powerapps.com/environments/Default-tenant/apps", "Power Apps Portal", "environments", "apps")]
        [InlineData("https://make.powerapps.com/environments/Default-tenant/solutions", "Power Apps Portal", "environments", "solutions")]
        [InlineData("https://invalid-url", "Unknown", "Unknown", "Unknown")]
        [InlineData("", "Unknown", "Unknown", "Unknown")]
        public void TestAppUrlParsing(string url, string expectedAppType, string expectedPageType, string expectedEntityName)
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var testRunSummary = new TestRunSummary(mockFileSystem.Object);

            // Act
            var result = testRunSummary.GetAppTypeAndEntityFromUrl(url);

            // Get tuple values using reflection
            var resultType = result.GetType();
            var appType = resultType.GetField("Item1").GetValue(result) as string;
            var pageType = resultType.GetField("Item2").GetValue(result) as string;
            var entityName = resultType.GetField("Item3").GetValue(result) as string;

            // Assert
            Assert.Equal(expectedAppType, appType);
            Assert.Equal(expectedPageType, pageType);
            Assert.Equal(expectedEntityName, entityName);
        }

        [Fact]
        public void TestAppUrlParsingWithNull()
        {
            // Arrange
            var mockFileSystem = new Mock<IFileSystem>();
            var testRunSummary = new TestRunSummary(mockFileSystem.Object);

            // Act
            var result = testRunSummary.GetAppTypeAndEntityFromUrl(null);

            // Get tuple values using reflection
            var resultType = result.GetType();
            var appType = resultType.GetField("Item1").GetValue(result) as string;
            var pageType = resultType.GetField("Item2").GetValue(result) as string;
            var entityName = resultType.GetField("Item3").GetValue(result) as string;

            // Assert
            Assert.Equal("Unknown", appType);
            Assert.Equal("Unknown", pageType);
            Assert.Equal("Unknown", entityName);
        }
    }
}
