using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
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

            var catalog = loader.LoadModules(setting, "");

            Assert.NotNull(catalog);
            Assert.Equal(0, catalog.Catalogs.Count);
        }

        [Theory]
        [InlineData("", "", "", "AssemblyCatalog")]
        [InlineData("*", "foo", "testengine.module.foo.dll", "AssemblyCatalog")]
        [InlineData("foo", "*", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        [InlineData("Foo", "*", "testengine.module.foo.dll", "AssemblyCatalog,AssemblyCatalog")]
        [InlineData("*", "foo*", "testengine.module.foo1.dll", "AssemblyCatalog")]
        public void ModuleMatch(string allow, string deny, string files, string expected)
        {
            var setting = new TestSettingExtensions() { Enable = true };
            var loader = new TestEngineModuleMEFLoader(MockLogger.Object);
            loader.DirectoryGetFiles = (location, pattern) => files.Split(",");
            // Use current test assembly as test
            loader.LoadAssembly = (file) => new AssemblyCatalog(this.GetType().Assembly);

            if ( !string.IsNullOrEmpty(allow) )
            {
                setting.AllowModule.Add(allow);
            }

            if (!string.IsNullOrEmpty(deny))
            {
                setting.DenyModule.Add(deny);
            }

            var catalog = loader.LoadModules(setting, Path.GetDirectoryName(this.GetType().Assembly.Location));

            Assert.NotNull(catalog);
            Assert.Equal(expected, string.Join(",",catalog.Catalogs.Select(c => c.GetType().Name)));
        }
    }
}
