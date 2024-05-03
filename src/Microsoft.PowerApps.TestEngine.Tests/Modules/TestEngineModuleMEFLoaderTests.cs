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
            Assert.Equal(0, catalog.Catalogs.Count);
        }

        [Theory]
        [InlineData(false, false, "", "", "", "AssemblyCatalog")]
        [InlineData(false, false, "*", "foo", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData(false, false, "foo", "*", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        [InlineData(false, false, "Foo", "*", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
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
    }
}
