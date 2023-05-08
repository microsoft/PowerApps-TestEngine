using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.Tests.Modules
{
    public class TestEngineExtensionCheckerTests
    {
        Mock<ILogger> MockLogger;
        string _template;

        public TestEngineExtensionCheckerTests()
        {
            MockLogger = new Mock<ILogger>();
            _template = @"
#r ""Microsoft.PowerFx.Interpreter.dll""
#r ""System.ComponentModel.Composition.dll""
#r ""Microsoft.PowerApps.TestEngine.dll""
#r ""Microsoft.Playwright.dll""
using System.Threading.Tasks;
using Microsoft.PowerFx;
using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.Playwright;
%USING%

[Export(typeof(ITestEngineModule))]
public class SampleModule : ITestEngineModule
{
    public void ExtendBrowserContextOptions(BrowserNewContextOptions options, TestSettings settings)
    {

    }

    public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, IPowerAppFunctions powerAppFunctions, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
    {
        var temp = new TestScript();
    }

    public async Task RegisterNetworkRoute(ITestState state, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, IPage Page, NetworkRequestMock mock)
    {
        await Task.CompletedTask;
    }
}
public class TestScript {
    public TestScript() {
        %CODE%
    }

}";
        }

        [Theory]
        [InlineData("", "System.Console.WriteLine(\"Hello World\");", true, "", "", true)]
        [InlineData("", "System.Console.WriteLine(\"Hello World\");", true, "", "System.", false)] // Deny all System namespace
        [InlineData("using System;", "Console.WriteLine(\"Hello World\");", true, "System.Console::WriteLine", "System.Console", true)]
        [InlineData("using System;", "Console.WriteLine(\"A\");", true, "System.Console::WriteLine(\"A\")", "System.Console::WriteLine", true)] // Allow System.Console.WriteLine only with a argument of A
        [InlineData("using System;", "Console.WriteLine(\"B\");", true, "System.Console::WriteLine(\"A\")", "System.Console::WriteLine", false)] // Allow System.Console.WriteLine only with a argument of A - Deny
        [InlineData("using System.IO;", @"File.Exists(""c:\\test.txt"");", true, "", "System.IO", false)] // Deny all System.IO
        [InlineData("", @"IPage page = null; page.EvaluateAsync(""alert()"").Wait();", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Constructor code - deny
        [InlineData("", @"} public string Foo { get { IPage page = null; page.EvaluateAsync(""alert()"").Wait(); return ""a""; }", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Get Property Code deny
        [InlineData("", @"} private int _foo; public int Foo { set { IPage page = null; page.EvaluateAsync(""alert()"").Wait(); _foo = value; }", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Set property deny
        [InlineData(@"using System; public class Other { private Action Foo {get;set; } = () => { IPage page = null; page.EvaluateAsync(""alert()"").Wait(); }; }", "", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Action deny property
        [InlineData(@"using System; public class Other { private Action Foo = () => { IPage page = null; page.EvaluateAsync(""alert()"").Wait(); }; }", "", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Action deny field
        [InlineData("using System; public class Other { private string Foo = String.Format(\"A\"); }", "", true, "", "System.String::Format", false)] // Action deny field - Inline Function
        [InlineData(@"using System; public class Other { private static Action Foo {get;set; } = () => { IPage page = null; page.EvaluateAsync(""alert()"").Wait(); }; }", "", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Static Action deny property
        [InlineData(@"using System; public class Other { private static Action Foo = () => { IPage page = null; page.EvaluateAsync(""alert()"").Wait(); }; }", "", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Static Action deny field
        [InlineData(@"using System; public class Other { static Other() { page.EvaluateAsync(""alert()"").Wait(); } static readonly IPage page = null; }", "", true, "", "Microsoft.Playwright.IPage::EvaluateAsync", false)] // Static Constructor
        public void IsValid(string usingStatements, string script, bool useTemplate, string allow, string deny, bool expected)
        {
            var assembly = CompileScript(useTemplate ? _template
                .Replace("%USING%", usingStatements)
                .Replace("%CODE%", script) : script);

            var checker = new TestEngineExtensionChecker(MockLogger.Object);
            checker.GetExtentionContents = (file) => assembly;

            var settings = new TestSettingExtensions()
            {
                Enable = true,
                AllowNamespaces = new List<string>() { allow },
                DenyNamespaces = new List<string>() { deny }
            };

            var result = checker.Validate(settings, "testengine.module.test.dll");

            Assert.Equal(expected, result);
        }

        private byte[] CompileScript(string script)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(script);
            ScriptOptions options = ScriptOptions.Default;
            var roslynScript = CSharpScript.Create(script, options);
            var compilation = roslynScript.GetCompilation();

            compilation = compilation.WithOptions(compilation.Options
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream())
            {
                var result = compilation.Emit(assemblyStream);
                if (!result.Success)
                {
                    var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(x => x));
                    throw new Exception("Compilation errors: " + Environment.NewLine + errors);
                }

                GC.Collect();
                return assemblyStream.ToArray();
            }
        }
    }
}
