// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            if (settings.Parameters.ContainsKey("AllowUntrustedRoot"))
            {
                allowUntrustedRoot = bool.Parse(settings.Parameters["AllowUntrustedRoot"]);
            }

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
        /// Validate that the provided file is allowed or should be denied based on the test settings
        /// </summary>
        /// <param name="settings">The test settings that should be evaluated</param>
        /// <param name="file">The .Net Assembly file to validate</param>
        /// <returns><c>True</c> if the assembly meets the test setting requirements, <c>False</c> if not</returns>
        public virtual bool Validate(TestSettingExtensions settings, string file)
        {
            var contents = GetExtentionContents(file);

            var allowList = new List<string>(settings.AllowNamespaces);
            // Add minimum namespaces for a MEF plugin used by TestEngine
            allowList.Add("System.Threading.Tasks");
            allowList.Add("Microsoft.PowerFx");
            allowList.Add("System.ComponentModel.Composition");
            allowList.Add("Microsoft.Extensions.Logging");
            allowList.Add("Microsoft.PowerApps.TestEngine.");
            allowList.Add("Microsoft.Playwright");

            var denyList = new List<string>(settings.DenyNamespaces);

            var found = LoadTypes(contents);

            var valid = true;

            if (!VerifyContainsValidNamespacePowerFxFunctions(settings, contents))
            {
                Logger.LogInformation("Invalid Power FX Namespace");
                valid = false;
            }

            foreach (var item in found)
            {
                // Allow if what was found is shorter and starts with allow value or what was found is a subset of a more specific allow rule
                var allowLongest = allowList.Where(a => item.StartsWith(a) || (item.Length < a.Length && a.StartsWith(item))).OrderByDescending(a => a.Length).FirstOrDefault();
                var denyLongest = denyList.Where(d => item.StartsWith(d)).OrderByDescending(d => d.Length).FirstOrDefault();
                var allow = !String.IsNullOrEmpty(allowLongest);
                var deny = !String.IsNullOrEmpty(denyLongest);

                if (allow && deny && denyLongest?.Length > allowLongest?.Length || !allow && deny)
                {
                    _logger.LogInformation("Deny usage of " + item);
                    _logger.LogInformation("Allow rule " + allowLongest);
                    _logger.LogInformation("Deny rule " + denyLongest);
                    valid = false;
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
            using (var stream = new MemoryStream(assembly))
            {
                stream.Position = 0;
                ModuleDefinition module = ModuleDefinition.ReadModule(stream);

                // Get the source code of the assembly as will be used to check Power FX Namespaces
                var code = DecompileModuleToCSharp(assembly);

                foreach (TypeDefinition type in module.GetAllTypes())
                {
                    if (type.BaseType != null && type.BaseType.Name == "ReflectionFunction")
                    {
                        var constructors = type.GetConstructors();

                        if (constructors.Count() == 0)
                        {
                            Logger.LogInformation($"No constructor defined for {type.Name}. Found {constructors.Count()} expected 1 or more");
                            return false;
                        }

                        var constructor = constructors.Where(c => c.HasBody).FirstOrDefault();

                        if (constructor == null)
                        {
                            Logger.LogInformation($"No constructor with a body");
                        }

                        if (!constructor.HasBody)
                        {
                            Logger.LogInformation($"No body defined for {type.Name}");
                            // Needs body for call to base constructor
                            return false;
                        }

                        var baseCall = constructor.Body.Instructions.FirstOrDefault(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference && ((MethodReference)i.Operand).Name == ".ctor");

                        if (baseCall == null)
                        {
                            Logger.LogInformation($"No base constructor defined for {type.Name}");
                            // Unable to find base constructor call
                            return false;
                        }

                        MethodReference baseConstructor = (MethodReference)baseCall.Operand;

                        if (baseConstructor.Parameters.Count() < 2)
                        {
                            // Not enough parameters
                            Logger.LogInformation($"No not enough parameters for {type.Name}");
                            return false;
                        }

                        if (baseConstructor.Parameters[0].ParameterType.FullName != "Microsoft.PowerFx.Core.Utils.DPath")
                        {
                            // First argument should be Namespace
                            Logger.LogInformation($"No Power FX Namespace for {type.Name}");
                            return false;
                        }

                        // Use the decompiled code to get the values of the base constructor, specifically look for the namespace
                        var name = GetPowerFxNamespace(type.Name, code);

                        if (string.IsNullOrEmpty(name))
                        {
                            // No Power FX Namespace found
                            Logger.LogInformation($"No Power FX Namespace found for {type.Name}");
                            return false;
                        }

                        if (settings.DenyPowerFxNamespaces.Contains(name))
                        {
                            // Deny list match
                            Logger.LogInformation($"Deny Power FX Namespace {name} for {type.Name}");
                            return false;
                        }

#if DEBUG
                        // Add Experimenal namespaes in Debug compile it it has not been added in allow list
                        if (!settings.AllowPowerFxNamespaces.Contains("Experimental"))
                        {
                            settings.AllowPowerFxNamespaces.Add("Experimental");
                        }
#endif

                        if ((settings.DenyPowerFxNamespaces.Contains("*") && (
                                !settings.AllowPowerFxNamespaces.Contains(name) ||
                                (!settings.AllowPowerFxNamespaces.Contains(name) && name != "TestEngine")
                                )
                            ))
                        {
                            // Deny wildcard exists only. Could not find match in allow list and name was not reserved name TestEngine
                            Logger.LogInformation($"Deny Power FX Namespace {name} for {type.Name}");
                            return false;
                        }

                        if (!settings.AllowPowerFxNamespaces.Contains(name) && name != "TestEngine")
                        {
                            Logger.LogInformation($"Not allow Power FX Namespace {name} for {type.Name}");
                            // Not in allow list or the Reserved TestEngine namespace
                            return false;
                        }
                    }
                }
            }
            return isValid;
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
                    AddType(type, found);

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
