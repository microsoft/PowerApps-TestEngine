// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Modules
{
    public class TestEngineModuleMEFLoaderTests
    {
        Mock<ILogger> MockLogger;

        public TestEngineModuleMEFLoaderTests()
        {
            MockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void DisabledEmptyCatalog()
        {
            var setting = new TestSettingExtensions() { Enable = false };
            var loader = new TestEngineModuleMEFLoader(MockLogger.Object);

            var catalog = loader.LoadModules(setting);

            Assert.NotNull(catalog);
            Assert.Empty(catalog.Catalogs);
        }

        [Theory]
        [InlineData(false, false, "", "", "", "AssemblyCatalog")]
        [InlineData(false, false, "*", "foo", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "foo", "*", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "Foo", "*", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "*", "foo*", "testengine.module.foo1.dll", "AssemblyCatalog")]
        [InlineData(true, false, "*", "", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(true, true, "*", "", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        public void ModuleMatch(bool checkAssemblies, bool checkResult, string allow, string deny, string files, string expected)
        {
            var setting = new TestSettingExtensions()
            {
                Enable = true,
                CheckAssemblies = checkAssemblies
            };
            Mock<TestEngineExtensionChecker> mockChecker = new Mock<TestEngineExtensionChecker>();

            var loader = new TestEngineModuleMEFLoader(MockLogger.Object);
            loader.DirectoryGetFiles = (location, pattern) =>
            {
                var searchPattern = Regex.Escape(pattern).Replace(@"\*", ".*?");
                return files.Split(",").Where(f => Regex.IsMatch(f, searchPattern)).ToArray();
            };
            // Use current test assembly as test
            loader.LoadAssembly = (file) => new AssemblyCatalog(this.GetType().Assembly);
            loader.Checker = mockChecker.Object;

            if (checkAssemblies)
            {
                mockChecker.Setup(x => x.Validate(It.IsAny<TestSettingExtensions>(), files)).Returns(checkResult);
            }

            if (!string.IsNullOrEmpty(allow))
            {
                setting.AllowModule.Add(allow);
            }

            if (!string.IsNullOrEmpty(deny))
            {
                setting.DenyModule.Add(deny);
            }

            var catalog = loader.LoadModules(setting);

            Assert.NotNull(catalog);
            Assert.Equal(expected, string.Join(",", catalog.Catalogs.Select(c => c.GetType().Name)));
        }

        [Theory]
        [InlineData("provider", true, true)]
        [InlineData("provider", true, false)]
        [InlineData("provider", false, false)]
        [InlineData("user", true, true)]
        [InlineData("user", true, false)]
        [InlineData("user", false, false)]
        public void ProviderMatch(string providerType, bool verify, bool valid)
        {
            // Arrange
            var assemblyName = $"testengine.{providerType}.test.dll";

            var setting = new TestSettingExtensions()
            {
                Enable = true,
                CheckAssemblies = true
            };
            Mock<TestEngineExtensionChecker> mockChecker = new Mock<TestEngineExtensionChecker>();

            var loader = new TestEngineModuleMEFLoader(MockLogger.Object);
            loader.DirectoryGetFiles = (location, pattern) =>
            {
                var searchPattern = Regex.Escape(pattern).Replace(@"\*", ".*?");
                return pattern.Contains(providerType) ? new List<string>() { assemblyName }.ToArray() : new string[] { };
            };

            mockChecker.Setup(m => m.ValidateProvider(setting, assemblyName)).Returns(verify);
            mockChecker.Setup(m => m.Verify(setting, assemblyName)).Returns(valid);

            if (valid)
            {
                // Use current test assembly as test
                loader.LoadAssembly = (file) => new AssemblyCatalog(this.GetType().Assembly);
            }

            loader.Checker = mockChecker.Object;

            // Act
            var catalog = loader.LoadModules(setting);

            // Assert
            if (verify && valid)
            {
                Assert.NotNull(catalog);
                Assert.Equal("AssemblyCatalog,AssemblyCatalog", string.Join(",", catalog.Catalogs.Select(c => c.GetType().Name)));
            }
            else
            {
                Assert.Equal("AssemblyCatalog", string.Join(",", catalog.Catalogs.Select(c => c.GetType().Name)));
            }
        }
    }
}
