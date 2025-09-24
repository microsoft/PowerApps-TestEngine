// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

namespace Microsoft.PowerApps.TestEngine.Modules
{
    /// <summary>
    /// Check that references, types and called methods are allowed or denied 
    /// The assembly to be checked is not loaded into the AppDomain is is loaded with definition only for checks
    /// </summary>
    public class TestEngineExtensionChecker
    {
        ILogger _logger;

        public Func<string, byte[]> GetExtentionContents = (file) => File.ReadAllBytes(file);

        public const string NAMESPACE_PREVIEW = "Preview";
        public const string NAMESPACE_TEST_ENGINE = "TestEngine";
        public const string NAMESPACE_DEPRECATED = "Deprecated";
        public const string SELFREFERENCE_NAMESPACE = "<module>";

        private static readonly HashSet<string> AllowedNamespaces = InitializeAllowedNamespaces();
        private static HashSet<string> InitializeAllowedNamespaces()
        {
            var allowedNamespaces = new HashSet<string>();
            var resourceManager = new ResourceManager(typeof(NamespaceResource));
            var resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);

            foreach (DictionaryEntry entry in resourceSet)
            {
                allowedNamespaces.Add(entry.Value.ToString());
            }
            return allowedNamespaces;
        }

        public TestEngineExtensionChecker()
        {

        }

        public TestEngineExtensionChecker(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger Logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        public Func<bool> CheckCertificates = () => VerifyCertificates();

        /// <summary>
        /// Verify that the provided file is signed by a trusted X509 root certificate authentication provider and the certificate is still valid
        /// </summary>
        /// <param name="settings">The test settings that should be evaluated</param>
        /// <param name="file">The .Net Assembly file to validate</param>
        /// <returns><c>True</c> if the assembly can be verified, <c>False</c> if not</returns>
        public virtual bool Verify(TestSettingExtensions settings, string file)
        {
            if (!CheckCertificates())
            {
                return true;
            }

            var cert = X509Certificate.CreateFromSignedFile(file);
            var cert2 = new X509Certificate2(cert.GetRawCertData());


            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;

            var valid = true;
            chain.Build(cert2);

            var sources = GetTrustedSources(settings);

            var allowUntrustedRoot = false;
#if RELEASE
            //dont allow untrusted
#else
            if (settings.Parameters.ContainsKey("AllowUntrustedRoot"))
            {
                allowUntrustedRoot = bool.Parse(settings.Parameters["AllowUntrustedRoot"]);
            }
#endif

            foreach (var elem in chain.ChainElements)
            {
                foreach (var status in elem.ChainElementStatus)
                {
                    if (status.Status == X509ChainStatusFlags.UntrustedRoot && allowUntrustedRoot)
                    {
                        continue;
                    }
                    valid = false;
                }
            }

            // Check if the chain of certificates is valid
            if (!valid)
            {
                return false;
            }

            // Check for valid trust sources
            foreach (var elem in chain.ChainElements)
            {
                foreach (var source in sources)
                {
                    if (!string.IsNullOrEmpty(source.Name) && elem.Certificate.IssuerName.Name.IndexOf($"CN={source.Name}") == -1)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(source.Organization) && elem.Certificate.IssuerName.Name.IndexOf($"O={source.Organization}") == -1)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(source.Location) && elem.Certificate.IssuerName.Name.IndexOf($"L={source.Location}") == -1)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(source.State) && elem.Certificate.IssuerName.Name.IndexOf($"S={source.State}") == -1)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(source.Country) && elem.Certificate.IssuerName.Name.IndexOf($"C={source.Country}") == -1)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(source.Thumbprint) && elem.Certificate.Thumbprint != source.Thumbprint)
                    {
                        continue;
                    }
                    // Found a trusted source
                    return true;
                }
            }
            return false;
        }

        private static bool VerifyCertificates()
        {
#if RELEASE
            return true;
#else
            return false;
#endif
        }

        private List<TestEngineTrustSource> GetTrustedSources(TestSettingExtensions settings)
        {
            var sources = new List<TestEngineTrustSource>();

            sources.Add(new TestEngineTrustSource()
            {
                Name = "Microsoft Root Certificate Authority",
                Organization = "Microsoft Corporation",
                Location = "Redmond",
                State = "Washington",
                Country = "US",
                Thumbprint = "8F43288AD272F3103B6FB1428485EA3014C0BCFE"
            });

            if (settings.Parameters.ContainsKey("TrustedSource"))
            {
                var parts = settings.Parameters["TrustedSource"].Split(',');
                var name = string.Empty;
                var organization = string.Empty;
                var location = string.Empty;
                var state = string.Empty;
                var country = string.Empty;
                var thumbprint = string.Empty;

                foreach (var part in parts)
                {
                    var nameValue = part.Trim().Split('=');
                    switch (nameValue[0])
                    {
                        case "CN":
                            name = nameValue[1];
                            break;
                        case "O":
                            organization = nameValue[1];
                            break;
                        case "L":
                            location = nameValue[1];
                            break;
                        case "S":
                            state = nameValue[1];
                            break;
                        case "C":
                            country = nameValue[1];
                            break;
                        case "T":
                            thumbprint = nameValue[1];
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(name))
                {
                    sources.Add(new TestEngineTrustSource()
                    {
                        Name = name,
                        Organization = organization,
                        Location = location,
                        State = state,
                        Country = country,
                        Thumbprint = thumbprint
                    });
                }
            }

            return sources;
        }

        /// <summary>
        /// Validate that the provided provider file is allowed or should be denied based on the test settings
        /// </summary>
        /// <param name="settings">The test settings that should be evaluated</param>
        /// <param name="file">The .Net Assembly file to validate</param>
        /// <returns><c>True</c> if the assembly meets the test setting requirements, <c>False</c> if not</returns>
        public virtual bool ValidateProvider(TestSettingExtensions settings, string file)
        {
            byte[] contents = GetExtentionContents(file);
            return VerifyContainsValidNamespacePowerFxFunctions(settings, contents);
        }

        /// <summary>
        /// Validate that the provided file is allowed or should be denied based on the test settings
        /// </summary>
        /// <param name="settings">The test settings that should be evaluated</param>
        /// <param name="file">The .Net Assembly file to validate</param>
        /// <returns><c>True</c> if the assembly meets the test setting requirements, <c>False</c> if not</returns>
        public virtual bool Validate(TestSettingExtensions settings, string file)
        {
            var allowList = new HashSet<string>(settings.AllowNamespaces);

            allowList.UnionWith(AllowedNamespaces);

            var denyList = new HashSet<string>(settings.DenyNamespaces)
            {
                "Microsoft.PowerApps.TestEngine.Modules.",
            };

            byte[] contents = GetExtentionContents(file);
            //ignore generic types
            var found = LoadTypes(contents).Where(item => !item.StartsWith("!")).ToList();

            var valid = true;

            if (!VerifyContainsValidNamespacePowerFxFunctions(settings, contents))
            {
                Logger.LogInformation("Invalid Power FX Namespace");
                valid = false;
                return valid;
            }

            foreach (var item in found)
            {
                // Allow if what was found is shorter and starts with allow value or what was found is a subset of a more specific allow rule
                var allowLongest = allowList.Where(a => item.StartsWith(a) || (item.Length < a.Length && a.StartsWith(item))).OrderByDescending(a => a.Length).FirstOrDefault();
                var denyLongest = denyList.Where(d => item.StartsWith(d)).OrderByDescending(d => d.Length).FirstOrDefault();
                var allow = !String.IsNullOrEmpty(allowLongest);
                var deny = !String.IsNullOrEmpty(denyLongest);

                if (allow && deny && denyLongest?.Length > allowLongest?.Length || !allow)
                {
                    _logger.LogInformation("Deny usage of " + item);
                    _logger.LogInformation("Allow rule " + allowLongest);
                    _logger.LogInformation("Deny rule " + denyLongest);
                    valid = false;
                    break;
                }
            }
            return valid;
        }

        /// <summary>
        /// Validate that the function only contains PowerFx functions that belong to valid Power Fx namespaces
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public bool VerifyContainsValidNamespacePowerFxFunctions(TestSettingExtensions settings, byte[] assembly)
        {
            var isValid = true;

#if DEBUG
            // Add Experimenal namespaces in Debug compile if it has not been added in allow list
            if (!settings.AllowPowerFxNamespaces.Contains(NAMESPACE_PREVIEW))
            {
                settings.AllowPowerFxNamespaces.Add(NAMESPACE_PREVIEW);
            }
#endif

#if RELEASE
            // Add Deprecated namespaces in Release compile if it has not been added in deny list
            if (!settings.DenyPowerFxNamespaces.Contains(NAMESPACE_DEPRECATED))
            {
                settings.DenyPowerFxNamespaces.Add(NAMESPACE_DEPRECATED);
            }
#endif

            using (var stream = new MemoryStream(assembly))
            {
                stream.Position = 0;
                ModuleDefinition module = ModuleDefinition.ReadModule(stream);

                // Detect if this assembly contains a provider/user/auth implementation
                bool assemblyHasProvider = module.GetAllTypes().Any(t =>
                    t.Interfaces.Any(i => i.InterfaceType.FullName == typeof(Providers.ITestWebProvider).FullName) ||
                    t.Interfaces.Any(i => i.InterfaceType.FullName == typeof(Users.IUserManager).FullName) ||
                    t.Interfaces.Any(i => i.InterfaceType.FullName == typeof(Config.IUserCertificateProvider).FullName));

                // Get the source code of the assembly as will be used to check Power FX Namespaces
                var code = DecompileModuleToCSharp(assembly);

                foreach (TypeDefinition type in module.GetAllTypes())
                {
                    // Provider checks are based on Namespaces string[] property
                    if (
                        type.Interfaces.Any(i => i.InterfaceType.FullName == typeof(Providers.ITestWebProvider).FullName)
                        ||
                        type.Interfaces.Any(i => i.InterfaceType.FullName == typeof(Users.IUserManager).FullName)
                        ||
                        type.Interfaces.Any(i => i.InterfaceType.FullName == typeof(Config.IUserCertificateProvider).FullName)
                        )
                    {
                        if (CheckPropertyArrayContainsValue(type, "Namespaces", out var values))
                        {
                            foreach (var name in values)
                            {
                                // Ignore Preview namespace for provider loading if not explicitly allowed to avoid blocking provider registration in Release builds.
                                if (name == NAMESPACE_PREVIEW && !settings.AllowPowerFxNamespaces.Contains(NAMESPACE_PREVIEW))
                                {
                                    Logger.LogInformation($"Ignoring Preview namespace on provider {type.Name} (not enabled in allow list).");
                                    continue;
                                }
                                // Check against deny list using regular expressions
                                if (settings.DenyPowerFxNamespaces.Any(pattern => Regex.IsMatch(name, WildcardToRegex(pattern))))
                                {
                                    Logger.LogInformation($"Deny Power FX Namespace {name} for {type.Name}");
                                    return false;
                                }

                                // Check against deny wildcard and allow list using regular expressions
                                if (settings.DenyPowerFxNamespaces.Any(pattern => pattern == "*") &&
                                    (!settings.AllowPowerFxNamespaces.Any(pattern => Regex.IsMatch(name, WildcardToRegex(pattern))) &&
                                     name != NAMESPACE_TEST_ENGINE))
                                {
                                    Logger.LogInformation($"Deny Power FX Namespace {name} for {type.Name}");
                                    return false;
                                }

                                // Check against allow list using regular expressions
                                if (!settings.AllowPowerFxNamespaces.Any(pattern => Regex.IsMatch(name, WildcardToRegex(pattern))) &&
                                    name != NAMESPACE_TEST_ENGINE)
                                {
                                    Logger.LogInformation($"Not allow Power FX Namespace {name} for {type.Name}");
                                    return false;
                                }
                            }
                        }
                    }

                    // Extension Module Check are based on constructor
                    if (type.BaseType != null && type.BaseType.Name == "ReflectionFunction")
                    {
                        var constructors = type.GetConstructors();

                        if (constructors.Count() == 0)
                        {
                            Logger.LogInformation($"No constructor defined for {type.Name}. Found {constructors.Count()} expected 1 or more");
                            return false;
                        }
                        var constructor = constructors.FirstOrDefault(c => c.HasBody);
                        if (constructor == null || !constructor.HasBody)
                        {
                            Logger.LogInformation($"No constructor with a body for {type.Name}");
                            return false;
                        }
                        if (!constructor.IsPublic)
                        {
                            Logger.LogInformation($"Constructor must be public for {type.Name}");
                            return false;
                        }
                        var baseCall = constructor.Body.Instructions?.FirstOrDefault(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference mr && mr.Name == ".ctor");
                        if (baseCall == null)
                        {
                            Logger.LogInformation($"No base constructor defined for {type.Name}");
                            return false;
                        }
                        var baseConstructor = (MethodReference)baseCall.Operand;
                        if (baseConstructor.Parameters?.Count() < 2)
                        {
                            // Not enough parameters
                            Logger.LogInformation($"No not enough parameters for {type.Name}");
                            return false;
                        }

                        string name;
                        var hasNamespaceParam = baseConstructor.Parameters[0].ParameterType.FullName == "Microsoft.PowerFx.Core.Utils.DPath";
                        if (hasNamespaceParam)
                        {
                            name = GetPowerFxNamespace(type.Name, code);
                            if (string.IsNullOrEmpty(name))
                            {
                                Logger.LogInformation($"No Power FX Namespace found for {type.Name}");
                                return false;
                            }
                        }
                        else
                        {
                            // Root/global function (e.g. Pause). Infer via attribute if needed.
                            bool isPreview = false, isDeprecated = false;
                            foreach (var ca in type.CustomAttributes)
                            {
                                if (ca.AttributeType.FullName == "Microsoft.PowerApps.TestEngine.Modules.TestEngineFunctionAttribute")
                                {
                                    foreach (var p in ca.Properties)
                                    {
                                        if (p.Name == "IsPreview" && p.Argument.Value is bool pv) isPreview = pv;
                                        if (p.Name == "IsDeprecated" && p.Argument.Value is bool dp) isDeprecated = dp;
                                    }
                                }
                            }
                            name = isPreview ? NAMESPACE_PREVIEW : isDeprecated ? NAMESPACE_DEPRECATED : NAMESPACE_TEST_ENGINE;
                        }

                        // Simple optimization: if this is a provider assembly and Preview not enabled, silently skip Preview functions.
                        if (assemblyHasProvider && name == NAMESPACE_PREVIEW && !settings.AllowPowerFxNamespaces.Contains(NAMESPACE_PREVIEW))
                        {
                            Logger.LogInformation($"Skipping Preview validation for provider function {type.Name} (Preview not enabled).");
                            continue;
                        }

                        if (settings.DenyPowerFxNamespaces.Contains(name))
                        {
                            Logger.LogInformation($"Deny Power FX Namespace {name} for {type.Name}");
                            return false;
                        }
                        if (settings.DenyPowerFxNamespaces.Contains("*") && !settings.AllowPowerFxNamespaces.Contains(name) && name != NAMESPACE_TEST_ENGINE)
                        {
                            // Deny wildcard exists only. Could not find match in allow list and name was not reserved name TestEngine
                            Logger.LogInformation($"Deny Power FX Namespace {name} for {type.Name}");
                            return false;
                        }

                        if (!settings.AllowPowerFxNamespaces.Contains(name) && name != NAMESPACE_TEST_ENGINE)
                        {
                            Logger.LogInformation($"Do not allow Power FX Namespace {name} for {type.Name}");
                            // Not in allow list or the Reserved TestEngine namespace
                            return false;
                        }
                    }
                }
            }
            return isValid;
        }

        // Helper method to convert wildcard patterns to regular expressions
        private string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        private bool CheckPropertyArrayContainsValue(TypeDefinition typeDefinition, string propertyName, out string[] values)
        {
            values = null;

            // Find the property by name
            var property = typeDefinition.HasProperties ? typeDefinition.Properties.FirstOrDefault(p => p.Name == propertyName) : null;
            if (property == null)
            {
                return false;
            }

            // Get the property type and check if it's an array
            var propertyType = property.PropertyType as ArrayType;
            if (propertyType == null)
            {
                return false;
            }

            // Assuming the property has a getter method
            var getMethod = property.GetMethod;
            if (getMethod == null)
            {
                return false;
            }

            // Load the assembly and get the method body
            var methodBody = getMethod.Body;
            if (methodBody == null)
            {
                return false;
            }

            // Iterate through the instructions to find the array initialization
            foreach (var instruction in methodBody?.Instructions)
            {
                if (instruction.OpCode == OpCodes.Newarr)
                {
                    // Call the method to get array values
                    var arrayValues = GetArrayValuesFromInstruction(methodBody, instruction);
                    values = arrayValues.OfType<string>().ToArray(); // Ensure values are strings
                    return values.Length > 0;
                }
            }

            return false;
        }

        private object[] GetArrayValuesFromInstruction(MethodBody methodBody, Instruction newarrInstruction)
        {
            var values = new List<object>();
            var instructions = methodBody?.Instructions;
            int index = instructions?.IndexOf(newarrInstruction) ?? 0;

            // Iterate through the instructions following the 'newarr' instruction
            for (int i = index + 1; i < instructions?.Count; i++)
            {
                var instruction = instructions[i];

                // Look for instructions that store values in the array
                if (instruction.OpCode == OpCodes.Stelem_Ref ||
                    instruction.OpCode == OpCodes.Stelem_I4 ||
                    instruction.OpCode == OpCodes.Stelem_R4 ||
                    instruction.OpCode == OpCodes.Stelem_R8)
                {
                    // The value to be stored is usually pushed onto the stack before the Stelem instruction
                    var valueInstruction = instructions[i - 1];

                    // Extract the value based on the opcode
                    switch (valueInstruction.OpCode.Code)
                    {
                        case Code.Ldc_I4:
                            values.Add((int)valueInstruction.Operand);
                            break;
                        case Code.Ldc_R4:
                            values.Add((float)valueInstruction.Operand);
                            break;
                        case Code.Ldc_R8:
                            values.Add((double)valueInstruction.Operand);
                            break;
                        case Code.Ldstr:
                            values.Add((string)valueInstruction.Operand);
                            break;
                            // Add more cases as needed for other types
                    }
                }

                // Stop if we reach another array initialization or method end
                if (instruction.OpCode == OpCodes.Newarr || instruction.OpCode == OpCodes.Ret)
                {
                    break;
                }
            }

            return values.ToArray();
        }

        /// <summary>
        /// Get the declared Power FX Namespace assigned to a Power FX Reflection function
        /// </summary>
        /// <param name="name">The name of the ReflectionFunction to find</param>
        /// <param name="code">The decompiled source code to search</param>
        /// <returns>The DPath Name that has been declared from the code</returns>
        private string GetPowerFxNamespace(string name, string code)
        {
            /*
            It is assumed that the code will be formatted like the following examples

            public FooFunction()
			: base(DPath.Root.Append(new DName("Foo")), "Foo", FormulaType.Blank) {
		    }

            or 

            public OtherFunction(int start)
			: base(DPath.Root.Append(new DName("Other")), "Foo", FormulaType.Blank) {
		    }

            */

            var lines = code.Split('\n').ToList();

            var match = lines.Where(l => l.Contains($"public {name}(")).FirstOrDefault();

            if (match == null)
            {
                return String.Empty;
            }

            var index = lines.IndexOf(match);

            // Search for a DName that is Appended to the Root path as functions should be in a Power FX Namespace not the Root
            var baseDeclaration = "base(DPath.Root.Append(new DName(\"";

            // Search for the DName
            var declaration = lines[index + 1].IndexOf(baseDeclaration);

            if (declaration >= 0)
            {
                // Found a match
                var start = declaration + baseDeclaration.Length;
                var end = lines[index + 1].IndexOf("\"", start);
                // Extract the Power FX Namespace argument from the declaration
                return lines[index + 1].Substring(declaration + baseDeclaration.Length, end - start);
            }

            return String.Empty;
        }

        private string DecompileModuleToCSharp(byte[] assembly)
        {
            var fileName = "module.dll";
            using (var module = new MemoryStream(assembly))
            using (var peFile = new PEFile(fileName, module))
            using (var writer = new StringWriter())
            {
                var decompilerSettings = new DecompilerSettings()
                {
                    ThrowOnAssemblyResolveErrors = false,
                    DecompileMemberBodies = true,
                    UsingDeclarations = true
                };
                decompilerSettings.CSharpFormattingOptions.ConstructorBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;

                var resolver = new UniversalAssemblyResolver(this.GetType().Assembly.Location, decompilerSettings.ThrowOnAssemblyResolveErrors,
                    peFile.DetectTargetFrameworkId(), peFile.DetectRuntimePack(),
                    decompilerSettings.LoadInMemory ? PEStreamOptions.PrefetchMetadata : PEStreamOptions.Default,
                    decompilerSettings.ApplyWindowsRuntimeProjections ? MetadataReaderOptions.ApplyWindowsRuntimeProjections : MetadataReaderOptions.None);
                var decompiler = new CSharpDecompiler(peFile, resolver, decompilerSettings);
                return decompiler.DecompileWholeModuleAsString();
            }
        }

        /// <summary>
        /// Load all the types from the assembly using Intermediate Language (IL) mode only
        /// </summary>
        /// <param name="assembly">The byte representation of the assembly</param>
        /// <returns>The Dependencies, Types and Method calls found in the assembly</returns>
        private List<string> LoadTypes(byte[] assembly)
        {
            List<string> found = new List<string>();

            using (var stream = new MemoryStream(assembly))
            {
                stream.Position = 0;
                ModuleDefinition module = ModuleDefinition.ReadModule(stream);

                // Add each assembly reference
                foreach (var reference in module.AssemblyReferences)
                {
                    if (!found.Contains(reference.Name))
                    {
                        found.Add(reference.Name);
                    }
                }

                foreach (TypeDefinition type in module.GetAllTypes())
                {
                    //ignoring self reference additional ignore checks for specialcases might be needed 
                    if (!type.Name.ToLower().Equals(SELFREFERENCE_NAMESPACE))
                    {
                        AddType(type, found);
                    }

                    // Load each constructor parameter and types in the body
                    foreach (var constructor in type.GetConstructors())
                    {
                        if (constructor.HasParameters)
                        {
                            LoadParametersTypes(constructor.Parameters, found);
                        }
                        if (constructor.HasBody)
                        {
                            LoadMethodBodyTypes(constructor.Body, found);
                        }
                    }

                    // Load any fields
                    foreach (var field in type.Fields)
                    {
                        if (found.Contains(field.FieldType.FullName) && !field.FieldType.IsValueType)
                        {
                            found.Add(field.FieldType.FullName);
                        }
                    }

                    // ... properties with get/set body if they exist
                    foreach (var property in type.Properties)
                    {
                        if (found.Contains(property.PropertyType.FullName) && !property.PropertyType.IsValueType)
                        {
                            found.Add(property.PropertyType.FullName);
                        }
                        if (property.GetMethod != null)
                        {
                            if (property.GetMethod.HasBody)
                            {
                                LoadMethodBodyTypes(property.GetMethod.Body, found);
                            }
                        }
                        if (property.SetMethod != null)
                        {
                            if (property.SetMethod.HasBody)
                            {
                                LoadMethodBodyTypes(property.SetMethod.Body, found);
                            }
                        }
                    }

                    // and method parameters and types in the method body
                    foreach (var method in type.Methods)
                    {
                        if (method.HasParameters)
                        {
                            LoadParametersTypes(method.Parameters, found);
                        }

                        if (method.HasBody)
                        {
                            LoadMethodBodyTypes(method.Body, found);
                        }
                    }
                }

                return found;
            }
        }

        private void LoadParametersTypes(Mono.Collections.Generic.Collection<ParameterDefinition> paramInfo, List<string> found)
        {
            foreach (var parameter in paramInfo)
            {
                AddType(parameter.ParameterType.GetElementType(), found);
            }
        }

        private void AddType(TypeReference type, List<string> found)
        {
            if (!found.Contains(type.FullName) && !type.IsPrimitive)
            {
                found.Add(type.FullName);
            }
        }

        /// <summary>
        /// Add method body instructions to the found list
        /// </summary>
        /// <param name="body">The body instructions to be searched</param>
        /// <param name="found">The list of matching code found</param>
        private void LoadMethodBodyTypes(MethodBody body, List<String> found)
        {
            foreach (var variable in body.Variables)
            {
                AddType(variable.VariableType.GetElementType(), found);
            }
            foreach (var instruction in body.Instructions)
            {
                switch (instruction.OpCode.FlowControl)
                {
                    case FlowControl.Call:
                        var methodInfo = (IMethodSignature)instruction.Operand;
                        AddType(methodInfo.ReturnType, found);
                        var name = methodInfo.ToString();
                        if (name.IndexOf(" ") > 0)
                        {
                            // Remove the return type from the call definition
                            name = name.Substring(name.IndexOf(" ") + 1);
                            var start = name.IndexOf("(");
                            var args = name.Substring(start + 1, name.Length - start - 2).Split(',');
                            if (args.Length >= 1 && !string.IsNullOrEmpty(args[0]))
                            {
                                name = name.Substring(0, start) + GetArgs(args, instruction);
                            }
                        }
                        if (!found.Contains(name))
                        {
                            found.Add(name);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Convert call arguments into values
        /// </summary>
        /// <param name="args">The arguments to be converted</param>
        /// <param name="instruction">The call instruction that the arguments relate to</param>
        /// <returns>The call text with primative values or argument types</returns>
        private string GetArgs(string[] args, Instruction instruction)
        {
            StringBuilder result = new StringBuilder("(");

            for (var i = 0; i < args.Length; i++)
            {
                var argValue = GetCallArgument(i, args.Length, instruction);
                switch (args[i])
                {
                    case "System.String":
                        if (argValue.OpCode.Code == Code.Ldstr)
                        {
                            result.Append("\"");
                            result.Append(argValue.Operand.ToString());
                            result.Append("\"");
                        }
                        else
                        {
                            result.Append(args[i]);
                        }
                        break;
                    default:
                        result.Append(args[i]);
                        break;
                }
                if (i != args.Length - 1)
                {
                    result.Append(",");
                }
            }

            result.Append(")");
            return result.ToString();
        }

        /// <summary>
        /// Get an argument for a method. They should be the nth intruction loaded before the method call
        /// </summary>
        /// <param name="index">The argument instruction to load</param>
        /// <param name="argCount">The total number of arguments</param>
        /// <param name="instruction">The call instruction</param>
        /// <returns></returns>
        private Instruction GetCallArgument(int index, int argCount, Instruction instruction)
        {
            Instruction current = instruction;
            while (index < argCount)
            {
                current = current.Previous;
                index++;
            }
            return current;
        }
    }
}
