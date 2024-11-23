// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Moq;
using Xunit;
using CertificateRequest = System.Security.Cryptography.X509Certificates.CertificateRequest;

namespace Microsoft.PowerApps.TestEngine.Tests.Modules
{
    public class TestEngineExtensionCheckerTests
    {
        Mock<ILogger> MockLogger;
        string _template;

        const string TEST_WEB_PROVIDER = @"
#r ""Microsoft.PowerApps.TestEngine.dll""

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;

public class Test : ITestWebProvider
{{
    public ITestInfraFunctions? TestInfraFunctions {{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }}
    public ISingleTestInstanceState? SingleTestInstanceState {{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }}
    public ITestState? TestState {{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }}
    public ITestProviderState? ProviderState {{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }}

    public string Name => throw new NotImplementedException();

    public string CheckTestEngineObject => throw new NotImplementedException();

    public string[] Namespaces => new string[] {{ ""{0}"" }};

    public Task<bool> CheckIsIdleAsync()
    {{
        throw new NotImplementedException();
    }}

    public Task CheckProviderAsync()
    {{
        throw new NotImplementedException();
    }}

    public string GenerateTestUrl(string domain, string queryParams)
    {{
        throw new NotImplementedException();
    }}

    public Task<object> GetDebugInfo()
    {{
        throw new NotImplementedException();
    }}

    public int GetItemCount(ItemPath itemPath)
    {{
        throw new NotImplementedException();
    }}

    public T GetPropertyValueFromControl<T>(ItemPath itemPath)
    {{
        throw new NotImplementedException();
    }}

    public Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
    {{
        throw new NotImplementedException();
    }}

    public Task<bool> SelectControlAsync(ItemPath itemPath)
    {{
        throw new NotImplementedException();
    }}

    public Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
    {{
        throw new NotImplementedException();
    }}

    public Task<bool> TestEngineReady()
    {{
        throw new NotImplementedException();
    }}
}}
";

        const string TEST_USER_MODULE = @"
#r ""Microsoft.PowerApps.TestEngine.dll""
#r ""Microsoft.Playwright.dll""

using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;

public class Test : IUserManager
{{
    public string[] Namespaces => new string[] {{ ""{0}"" }};

    public string Name => throw new NotImplementedException();

    public int Priority => throw new NotImplementedException();

    public bool UseStaticContext => throw new NotImplementedException();

    public string Location {{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }}

    public Task LoginAsUserAsync(string desiredUrl, IBrowserContext context, ITestState testState, ISingleTestInstanceState singleTestInstanceState, IEnvironmentVariable environmentVariable, IUserManagerLogin userManagerLogin)
    {{
        throw new NotImplementedException();
    }}
}}
";

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
using Microsoft.PowerApps.TestEngine.Providers;
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

    public void RegisterPowerFxFunction(PowerFxConfig config, ITestInfraFunctions testInfraFunctions, ITestWebProvider webTestProvider, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem)
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

        [Theory]
        [InlineData("", "", "TestEngine", true)] // Empty Allow and Deny list
        [InlineData("TestEngine", "", "TestEngine", true)] // No deny list
        [InlineData("", "TestEngine", "TestEngine", false)] // Invalid in deby list
        [InlineData("TestEngine", "Other", "TestEngine", true)] // Valid in the allow list
        [InlineData("TestEngine", "Other", "Other", false)] // Exact match
        [InlineData("", "*", "Other", false)] // Any regex match
        [InlineData("", "O*", "Other", false)] // Regex match wildcard 
        [InlineData("", "Test?", "Test1", false)] // Single character match
        [InlineData("", "Test?More", "Test1More", false)] // Single character match in the middle
        [InlineData("T*", "", "Test1More", true)] // Allow wildcard
        [InlineData("T?2", "", "T12", true)] // Allow wildcard
        [InlineData("T?2", "T?3", "T12", true)] // Allow wildcard and not match deny
        public void IsValidProvider(string allow, string deny, string providerNamespace, bool expected)
        {
            // Arrange
            var assembly = CompileScript(String.Format(TEST_WEB_PROVIDER, providerNamespace));

            var checker = new TestEngineExtensionChecker(MockLogger.Object);
            checker.GetExtentionContents = (file) => assembly;

            var settings = new TestSettingExtensions()
            {
                Enable = true,
                AllowPowerFxNamespaces = new List<string>() { allow },
                DenyPowerFxNamespaces = new List<string>() { deny }
            };

            // Act
            var result = checker.ValidateProvider(settings, "testengine.provider.test.dll");

            Assert.Equal(expected, result);
        }


        [Theory]
        [InlineData("", "", "TestEngine", true)] // Empty Allow and Deny list
        [InlineData("TestEngine", "", "TestEngine", true)] // No deny list
        [InlineData("", "TestEngine", "TestEngine", false)] // Invalid in deby list
        [InlineData("TestEngine", "Other", "TestEngine", true)] // Valid in the allow list
        [InlineData("TestEngine", "Other", "Other", false)] // Exact match
        [InlineData("", "*", "Other", false)] // Any regex match
        [InlineData("", "O*", "Other", false)] // Regex match wildcard 
        [InlineData("", "Test?", "Test1", false)] // Single character match
        [InlineData("", "Test?More", "Test1More", false)] // Single character match in the middle
        [InlineData("T*", "", "Test1More", true)] // Allow wildcard
        [InlineData("T?2", "", "T12", true)] // Allow wildcard
        [InlineData("T?2", "T?3", "T12", true)] // Allow wildcard and not match deny
        public void IsValidUserModule(string allow, string deny, string providerNamespace, bool expected)
        {
            // Arrange
            var assembly = CompileScript(String.Format(TEST_USER_MODULE, providerNamespace));

            var checker = new TestEngineExtensionChecker(MockLogger.Object);
            checker.GetExtentionContents = (file) => assembly;

            var settings = new TestSettingExtensions()
            {
                Enable = true,
                AllowPowerFxNamespaces = new List<string>() { allow },
                DenyPowerFxNamespaces = new List<string>() { deny }
            };

            // Act
            var result = checker.ValidateProvider(settings, "testengine.user.test.dll");

            Assert.Equal(expected, result);
        }


        private string _functionTemplate = @"
#r ""Microsoft.PowerFx.Interpreter.dll""
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Microsoft.PowerFx.Core.Utils;

%CODE%";

        [Theory]
        [InlineData("Test", "", "public class FooFunction : ReflectionFunction { public FooFunction() : base(\"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", false)] // No namespace
        [InlineData("Test", "", "public class FooFunction : ReflectionFunction { public FooFunction() : base(DPath.Root, \"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", false)] // Root namespace
        [InlineData("Test", "", "public class FooFunction : ReflectionFunction { public FooFunction() : base(DPath.Root.Append(new DName(\"Test\")), \"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", true)] // Non Root namespace
        [InlineData("", "", "public class FooFunction : ReflectionFunction { public FooFunction() : base(DPath.Root.Append(new DName(\"TestEngine\")), \"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", true)] // Allow TestEngine namespace
        [InlineData("", "", "public class FooFunction : ReflectionFunction { private FooFunction() : base(DPath.Root.Append(new DName(\"TestEngine\")), \"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", false)] // Private constructor - Not allow
        [InlineData("", "*", "public class FooFunction : ReflectionFunction { public FooFunction() : base(DPath.Root.Append(new DName(\"Other\")), \"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", false)] // Deny all and not in allow list
        [InlineData("Other", "*", "public class FooFunction : ReflectionFunction { public FooFunction() : base(DPath.Root.Append(new DName(\"Other\")), \"Foo\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", true)] // Deny all and in allow list
        [InlineData("", "", "public class OtherFunction : ReflectionFunction { public OtherFunction(int someNumber) : base(DPath.Root.Append(new DName(\"TestEngine\")), \"Other\", FormulaType.Blank) {} } public BlankValue Execute() { return FormulaValue.NewBlank(); }", true)] // Allow TestEngine namespace with a parameter
        public void ValidPowerFxFunction(string allow, string deny, string code, bool valid)
        {
            // Arrange 
            var checker = new TestEngineExtensionChecker(MockLogger.Object);

            var assembly = CompileScript(_functionTemplate.Replace("%CODE%", code));

            var settings = new TestSettingExtensions();
            settings.AllowPowerFxNamespaces.AddRange(allow.Split(','));
            settings.DenyPowerFxNamespaces.AddRange(deny.Split(','));

            var isValid = checker.VerifyContainsValidNamespacePowerFxFunctions(settings, assembly);

            Assert.Equal(valid, isValid);
        }

        [Theory]
        [InlineData(false, true, false, "CN=Test", "CN=Test", 0, 1, true)]
        [InlineData(false, false, false, "", "", 0, 1, true)]
        [InlineData(true, true, true, "CN=Test", "CN=Test", -1, 1, true)] // Valid certificate
        [InlineData(true, true, false, "CN=Test", "CN=Test", -1, 1, false)] // Valid certificate but with untrusted root
        [InlineData(true, true, true, "CN=Test, O=Match", "CN=Test, O=Match", -1, 1, true)] // Valid certificate with O
        [InlineData(true, true, true, "CN=Test, O=Match", "CN=Test, O=Other", -1, 1, false)] // Organization mismatch
        [InlineData(true, true, true, "CN=Test, O=Match, S=WA", "CN=Test, O=Match, S=XX", -1, 1, false)] // State mismatch
        [InlineData(true, true, true, "CN=Test, O=Match, S=WA, C=US", "CN=Test, O=Match, S=WA, C=XX", -1, 1, false)] // Country mismatch
        [InlineData(true, true, true, "CN=Test", "CN=Test", -100, -1, false)] // Expired certificate
        public void Verify(bool checkCertificates, bool sign, bool allowUntrustedRoot, string signWith, string trustedSource, int start, int end, bool expectedResult)
        {
            var assembly = CompileScript("var i = 1;");

            byte[] dllBytes = assembly;
            if (sign)
            {
                // Generate a key pair
                X509Certificate2 certificate = GenerateSelfSignedCertificate(signWith, DateTime.Now.AddDays(start), DateTime.Now.AddDays(end));

                // Create a ContentInfo object from the DLL bytes
                ContentInfo contentInfo = new ContentInfo(dllBytes);

                // Create a SignedCms object
                SignedCms signedCms = new SignedCms(contentInfo);

                // Create a CmsSigner object
                CmsSigner cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate);

                // Sign the DLL bytes
                signedCms.ComputeSignature(cmsSigner);

                // Get the signed bytes
                byte[] signedBytes = signedCms.Encode();

                dllBytes = signedBytes;
            }

            var checker = new TestEngineExtensionChecker(MockLogger.Object);
            checker.CheckCertificates = () => checkCertificates;
            checker.GetExtentionContents = (file) => assembly;

            var settings = new TestSettingExtensions()
            {
                Enable = true
            };
            if (!string.IsNullOrEmpty(trustedSource))
            {
                settings.Parameters.Add("TrustedSource", trustedSource);
            }
            if (allowUntrustedRoot)
            {
                settings.Parameters.Add("AllowUntrustedRoot", "True");
            }

            var valid = false;
            try
            {
                var tempFile = $"test.deleteme.{Guid.NewGuid()}.dll";
                File.WriteAllBytes(tempFile, dllBytes);
                if (checker.Verify(settings, tempFile))
                {
                    valid = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Assert.Equal(expectedResult, valid);
        }

        // Method to generate a self-signed X509Certificate2
        static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, DateTime validFrom, DateTime validTo)
        {
            using (RSA rsa = RSA.Create(2048)) // Generate a 2048-bit RSA key
            {
                var certRequest = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Set certificate properties
                certRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                certRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
                certRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.3") }, false));
                certRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));

                // Create the self-signed certificate
                X509Certificate2 certificate = certRequest.CreateSelfSigned(validFrom, validTo);

                return certificate;
            }
        }

        public static byte[] CompileScript(string script)
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
