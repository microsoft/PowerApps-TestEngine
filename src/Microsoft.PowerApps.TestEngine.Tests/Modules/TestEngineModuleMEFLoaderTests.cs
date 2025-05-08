// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
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
        [InlineData(false, false, null, "", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, null, null, "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "", null, "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "", "", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(true, true, "", "", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        [InlineData(true, false, "", "", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "foo1", "foo2", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(true, true, "foo1,*", "foo2", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        [InlineData(false, false, "*", "foo", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "foo", "*", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "Foo", "*", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "*", "foo*", "testengine.module.foo1.dll", "AssemblyCatalog")]
        [InlineData(true, false, "*", "", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(true, true, "*", "", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        public void ModuleMatch(bool checkAssemblies, bool checkResult, string? allow, string? deny, string files, string expected)
        {
#if RELEASE
            if (!checkAssemblies)
            {
                //not a valid scenario since it cant be assigned
                return;
            }
#endif
            var setting = new TestSettingExtensions()
            {
                Enable = true,
#if RELEASE     
#else
                CheckAssemblies = checkAssemblies
#endif
            };
            Mock<TestEngineExtensionChecker> mockChecker = new Mock<TestEngineExtensionChecker>();

            var loader = new TestEngineModuleMEFLoader(MockLogger.Object);
            loader.DirectoryGetFiles = (location, pattern) =>
            {
                var searchPattern = Regex.Escape(pattern).Replace(@"\*", ".*?");
                return files.Split(',').Where(f => Regex.IsMatch(f, searchPattern)).ToArray();
            };
            // Use current test assembly as test
            loader.LoadAssembly = (file) => new AssemblyCatalog(this.GetType().Assembly);
            loader.Checker = mockChecker.Object;

            if (checkAssemblies)
            {
                mockChecker.Setup(x => x.Validate(It.IsAny<TestSettingExtensions>(), files)).Returns(checkResult);
                mockChecker.Setup(x => x.Verify(It.IsAny<TestSettingExtensions>(), files)).Returns(checkResult);
            }

            if (!string.IsNullOrEmpty(allow))
            {
                //assigning instead of adding since it replaces default * allow all
                var allowList = allow.Split(',').Select(a => a.Trim()).ToList();
                setting.AllowModule = new HashSet<string>(allowList);
            }
            if (allow == null)
            {
                setting.AllowModule = null;
            }

            if (!string.IsNullOrEmpty(deny))
            {
                //assigning instead of adding since it replaces default * allow all
                var denyList = deny.Split(',').Select(a => a.Trim()).ToList();
                setting.DenyModule = new HashSet<string>(denyList);
            }
            if (deny == null)
            {
                setting.DenyModule = null;
            }

            var catalog = loader.LoadModules(setting);

            Assert.NotNull(catalog);
            Assert.Equal(expected, string.Join(",", catalog.Catalogs.Select(c => c.GetType().Name)));
        }

        [Theory]
        [InlineData("provider", "mda", true, true)]
        [InlineData("provider", "test", true, false)]
        [InlineData("provider", "test", false, false)]
        [InlineData("user", "storagestate", true, true)]
        [InlineData("user", "test", true, false)]
        [InlineData("user", "test", false, false)]
        [InlineData("auth", "certstore", true, true, Skip = "No auth providers allowlisted for releases")]
        [InlineData("auth", "environment.certificate", true, true)]
        [InlineData("auth", "test", true, false)]
        [InlineData("auth", "test", false, false)]
        public void ProviderMatch(string providerType, string specificName, bool verify, bool valid)
        {
            // Arrange
            var assemblyName = $"testengine.{providerType}.{specificName}.dll";

            var setting = new TestSettingExtensions()
            {
                Enable = true,
#if RELEASE
#else
                CheckAssemblies = true
#endif
            };
            Mock<TestEngineExtensionChecker> mockChecker = new Mock<TestEngineExtensionChecker>();

            var loader = new TestEngineModuleMEFLoader(MockLogger.Object);
            loader.DirectoryGetFiles = (location, pattern) =>
            {
                var searchPattern = Regex.Escape(pattern).Replace(@"\*", ".*?");
                return pattern.Contains(providerType) ? new List<string>() { Path.Combine(location, assemblyName) }.ToArray() : new string[] { };
            };

            mockChecker.Setup(m => m.ValidateProvider(setting, It.Is<string>(p => p.Contains(assemblyName)))).Returns(verify);
            mockChecker.Setup(m => m.Verify(setting, It.Is<string>(p => p.Contains(assemblyName)))).Returns(valid);

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
