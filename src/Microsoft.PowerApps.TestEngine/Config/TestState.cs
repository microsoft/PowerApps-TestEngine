// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;

namespace Microsoft.PowerApps.TestEngine.Config
{
    /// <summary>
    /// State of the test run
    /// </summary>
    public class TestState : ITestState
    {
        private readonly ITestConfigParser _testConfigParser;
        private readonly IFileSystem _fileSystem;
        private ILogger _logger;
        private bool _recordMode = false;

        public event EventHandler<TestStepEventArgs> BeforeTestStepExecuted;
        public event EventHandler<TestStepEventArgs> AfterTestStepExecuted;

        private TestPlanDefinition TestPlanDefinition { get; set; }
        private List<TestCase> TestCases { get; set; } = new List<TestCase>();
        private string EnvironmentId { get; set; }
        private string Domain { get; set; }

        private string TenantId { get; set; }

        private string OutputDirectory { get; set; }

        private FileInfo TestConfigFile { get; set; }

        private string ModulePath { get; set; }

        private List<ITestEngineModule> Modules { get; set; } = new List<ITestEngineModule>();

        private List<IUserManager> UserManagers { get; set; } = new List<IUserManager>();

        private List<ITestWebProvider> WebProviders { get; set; } = new List<ITestWebProvider>();

        private List<IUserCertificateProvider> CertificateProviders { get; set; } = new List<IUserCertificateProvider>();

        private bool IsValid { get; set; } = false;

        // Determine if Power FX expressions delimited by ; should be executed step by step
        public bool ExecuteStepByStep { get; set; } = false;

        public ITestWebProvider TestProvider { get; set; }
        public TestState(ITestConfigParser testConfigParser)
        {
            _testConfigParser = testConfigParser;
            _fileSystem = new FileSystem();
        }

        public TestState(ITestConfigParser testConfigParser, IFileSystem fileSystem)
        {
            _testConfigParser = testConfigParser;
            _fileSystem = fileSystem;
        }

        public TestSuiteDefinition GetTestSuiteDefinition()
        {
            if (_recordMode)
            {
                return new TestSuiteDefinition
                {
                    RecordMode = true,
                    AppId = TestPlanDefinition?.TestSuite.AppId,
                    AppLogicalName = TestPlanDefinition?.TestSuite.AppLogicalName,
                    Persona = TestPlanDefinition?.TestSuite.Persona,
                };
            }

            return TestPlanDefinition?.TestSuite;
        }

        public List<TestCase> GetTestCases()
        {
            return TestCases;
        }
        public void ParseAndSetTestState(string testConfigFile, ILogger logger)
        {
            if (string.IsNullOrEmpty(testConfigFile))
            {
                throw new ArgumentNullException(nameof(testConfigFile));
            }

            _logger = logger;

            List<string> userInputExceptionMessages = new List<string>();
            try
            {
                TestPlanDefinition = _testConfigParser.ParseTestConfig<TestPlanDefinition>(testConfigFile, logger);
                if (TestPlanDefinition.TestSuite != null)
                {
                    TestCases = TestPlanDefinition.TestSuite.TestCases;

                    if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.TestSuiteName))
                    {
                        userInputExceptionMessages.Add("Missing test suite name from test suite definition");
                    }

                    if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.Persona))
                    {
                        userInputExceptionMessages.Add("Missing persona from test suite definition");
                    }

                    if (string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppLogicalName) && string.IsNullOrEmpty(TestPlanDefinition.TestSuite.AppId))
                    {
                        userInputExceptionMessages.Add("At least one of the app logical name or app id must be present in test suite definition");
                    }
                }

                if (TestCases == null || TestCases?.Count == 0)
                {
                    userInputExceptionMessages.Add("Must be at least one test case");
                }
                else
                {
                    foreach (var testCase in TestCases)
                    {
                        if (string.IsNullOrEmpty(testCase.TestCaseName))
                        {
                            userInputExceptionMessages.Add("Missing test case name from test definition");
                        }

                        if (string.IsNullOrEmpty(testCase.TestSteps))
                        {
                            userInputExceptionMessages.Add("Missing test steps from test case");
                        }
                    }
                }

                if (TestPlanDefinition.TestSettings == null)
                {
                    userInputExceptionMessages.Add("Missing test settings from test plan");
                }
                else if (!string.IsNullOrEmpty(TestPlanDefinition.TestSettings?.FilePath))
                {
                    var testSettingFile = TestPlanDefinition.TestSettings.FilePath;
                    if (!Path.IsPathRooted(testSettingFile))
                    {
                        // Generate a absolte path relative to the test file
                        testSettingFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testConfigFile), testSettingFile));
                    }
                    TestPlanDefinition.TestSettings = _testConfigParser.ParseTestConfig<TestSettings>(testSettingFile, logger);
                }

                if (TestPlanDefinition.TestSettings?.BrowserConfigurations == null
                    || TestPlanDefinition.TestSettings?.BrowserConfigurations?.Count == 0)
                {
                    userInputExceptionMessages.Add("Missing browser configuration from test plan");
                }
                else
                {
                    foreach (var browserConfig in TestPlanDefinition.TestSettings?.BrowserConfigurations)
                    {
                        if (string.IsNullOrWhiteSpace(browserConfig.Browser))
                        {
                            userInputExceptionMessages.Add("Missing browser from browser configuration");
                        }

                        if (browserConfig.ScreenWidth == null && browserConfig.ScreenHeight != null
                            || browserConfig.ScreenHeight == null && browserConfig.ScreenWidth != null)
                        {
                            userInputExceptionMessages.Add("Screen width and height both need to be specified or not specified");
                        }
                    }
                }

                if (TestPlanDefinition.EnvironmentVariables == null)
                {
                    userInputExceptionMessages.Add("Missing environment variables from test plan");
                }
                else if (!string.IsNullOrEmpty(TestPlanDefinition.EnvironmentVariables.FilePath))
                {
                    var testEnvironmentFile = TestPlanDefinition.EnvironmentVariables.FilePath;
                    if (!Path.IsPathRooted(testEnvironmentFile))
                    {
                        testEnvironmentFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testConfigFile), testEnvironmentFile));
                    }
                    TestPlanDefinition.EnvironmentVariables = _testConfigParser.ParseTestConfig<EnvironmentVariables>(testEnvironmentFile, logger);
                }

                if (TestPlanDefinition.EnvironmentVariables?.Users == null
                    || TestPlanDefinition.EnvironmentVariables?.Users?.Count == 0)
                {
                    userInputExceptionMessages.Add("At least one user must be specified");
                }
                else
                {
                    foreach (var userConfig in TestPlanDefinition.EnvironmentVariables?.Users)
                    {
                        if (string.IsNullOrEmpty(userConfig.PersonaName))
                        {
                            userInputExceptionMessages.Add("Missing persona name");
                        }

                        if (string.IsNullOrEmpty(userConfig.EmailKey))
                        {
                            userInputExceptionMessages.Add("Missing email key");
                        }
                    }
                }

                if (TestPlanDefinition.EnvironmentVariables?.Users?.Where(x => x.PersonaName == TestPlanDefinition.TestSuite?.Persona).FirstOrDefault() == null)
                {
                    userInputExceptionMessages.Add("Persona specified in test is not listed in environment variables");
                }
            }
            catch (UserInputException ex)
            {
                throw new UserInputException(ex.Message);
            }

            if (userInputExceptionMessages.Count() > 0)
            {
                logger.LogError($"Invalid User Input(s): {String.Join(", ", userInputExceptionMessages)}");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionTestConfig.ToString());
            }

            IsValid = true;
        }

        public void SetEnvironment(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                throw new ArgumentNullException(nameof(environmentId));
            }
            EnvironmentId = environmentId;
        }

        public string GetEnvironment()
        {
            return EnvironmentId;
        }

        public void SetDomain(string domain)
        {
            Domain = domain;
        }

        public string GetDomain()
        {
            return Domain;
        }

        public void SetTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }
            TenantId = tenantId;
        }

        public string GetTenant()
        {
            return TenantId;
        }
        public void SetOutputDirectory(string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }
            OutputDirectory = outputDirectory;
        }
        public string GetOutputDirectory()
        {
            return OutputDirectory;
        }

        public void SetTestConfigFile(FileInfo testConfig)
        {
            if (testConfig == null)
            {
                throw new ArgumentNullException(nameof(testConfig));
            }
            TestConfigFile = testConfig;
        }
        public FileInfo GetTestConfigFile()
        {
            return TestConfigFile;
        }

        public UserConfiguration GetUserConfiguration(string persona)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("TestPlanDefinition is not valid");
            }

            var userConfiguration = TestPlanDefinition?.EnvironmentVariables?.Users?.Where(x => x.PersonaName == persona).FirstOrDefault();

            if (userConfiguration == null)
            {
                throw new InvalidOperationException($"Unable to find user configuration for persona: {persona}");
            }

            return userConfiguration;
        }
        public TestSettings GetTestSettings()
        {
            var settings = TestPlanDefinition?.TestSettings;

            if (settings != null)
            {
                // If there's a test settings file specified, load and merge it
                if (!string.IsNullOrEmpty(settings.FilePath))
                {
                    LoadTestSettingsFile(settings.FilePath, settings);
                }

                // Process any PowerFx definition files
                ProcessPowerFxDefinitions(settings);
            }

            return settings;
        }
        private void LoadTestSettingsFile(string filePath, TestSettings parentSettings)
        {
            try
            {
                // If _logger is not set, we might be in a scenario where ParseAndSetTestState hasn't been called
                // In this case, we use a NullLogger to avoid null reference exceptions
                var logger = _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

                if (_fileSystem.FileExists(filePath))
                {
                    var fileSettings = _testConfigParser.ParseTestConfig<TestSettings>(filePath, logger);
                    MergeTestSettings(fileSettings, parentSettings);
                }
                else
                {
                    logger.LogWarning($"Test settings file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                // Similarly, use NullLogger if _logger is not set
                var logger = _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
                logger.LogError($"Error loading test settings file: {ex.Message}");
            }
        }

        private void MergeTestSettings(TestSettings source, TestSettings destination)
        {
            // Merge properties from source to destination, only if they're not already set
            if (!string.IsNullOrEmpty(source.Locale) && string.IsNullOrEmpty(destination.Locale))
            {
                destination.Locale = source.Locale;
            }

            if (source.Timeout != 30000 && destination.Timeout == 30000) // Default value check
            {
                destination.Timeout = source.Timeout;
            }

            if (source.RecordVideo && !destination.RecordVideo)
            {
                destination.RecordVideo = source.RecordVideo;
            }

            if (source.Headless != true && destination.Headless == true) // Default is true
            {
                destination.Headless = source.Headless;
            }

            // Merge browser configurations if not already set
            if ((destination.BrowserConfigurations == null || destination.BrowserConfigurations.Count == 0) &&
                source.BrowserConfigurations != null && source.BrowserConfigurations.Count > 0)
            {
                destination.BrowserConfigurations = source.BrowserConfigurations;
            }

            // Always merge PowerFx types and functions
            if (source.PowerFxTestTypes != null)
            {
                destination.PowerFxTestTypes.AddRange(source.PowerFxTestTypes);
            }

            if (source.TestFunctions != null)
            {
                destination.TestFunctions.AddRange(source.TestFunctions);
            }

            // Add any PowerFx definition files from the source
            if (source.PowerFxDefinitions != null)
            {
                if (destination.PowerFxDefinitions == null)
                {
                    destination.PowerFxDefinitions = new List<PowerFxDefinition>();
                }

                destination.PowerFxDefinitions.AddRange(source.PowerFxDefinitions);
            }
        }

        private void ProcessPowerFxDefinitions(TestSettings settings)
        {
            if (settings.PowerFxDefinitions == null || settings.PowerFxDefinitions.Count == 0)
            {
                return;
            }

            // If _logger is not set, use NullLogger to avoid null reference exceptions
            var logger = _logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            var loader = new PowerFxDefinitionLoader(_fileSystem, logger);
            string baseDirectory = TestConfigFile != null ?
                Path.GetDirectoryName(TestConfigFile.FullName) :
                Directory.GetCurrentDirectory();

            foreach (var definition in settings.PowerFxDefinitions)
            {
                if (string.IsNullOrEmpty(definition.Location))
                {
                    continue;
                }

                // Resolve path relative to the test file
                string resolvedPath;
                if (Path.IsPathRooted(definition.Location))
                {
                    resolvedPath = definition.Location;
                }
                else
                {
                    resolvedPath = Path.GetFullPath(Path.Combine(baseDirectory, definition.Location));
                }

                loader.LoadPowerFxDefinitionsFromFile(resolvedPath, settings);
            }
        }

        public int GetTimeout()
        {
            return GetTestSettings().Timeout;
        }

        public void SetModulePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            ModulePath = path;
        }

        /// <summary>
        /// Load Managed Extensibility Framework (MEF) Test Engine modules
        /// </summary>
        public void LoadExtensionModules(ILogger logger)
        {
            var loader = new TestEngineModuleMEFLoader(logger);
            var settings = this.GetTestSettings();
            var catalogModules = loader.LoadModules(settings.ExtensionModules);

            using var catalog = new AggregateCatalog(catalogModules);
            using var container = new CompositionContainer(catalog);

            var mefComponents = new MefComponents();
            container.ComposeParts(mefComponents);
            var components = mefComponents.MefModules.Select(v => v.Value).ToArray();
            this.AddModules(components);

            var userManagers = mefComponents.UserModules.Select(v => v.Value).OrderByDescending(v => v.Priority).ToArray();
            this.AddUserModules(userManagers);

            var webProviders = mefComponents.WebProviderModules.Select(v => v.Value).ToArray();
            this.AddWebProviderModules(webProviders);

            var certificateProviders = mefComponents.CertificateProviderModules.Select(v => v.Value).ToArray();
            this.AddCertificateProviders(certificateProviders);
        }

        public void AddModules(IEnumerable<ITestEngineModule> modules)
        {
            Modules.Clear();
            Modules.AddRange(modules);
        }

        public void AddUserModules(IEnumerable<IUserManager> modules)
        {
            UserManagers.Clear();
            UserManagers.AddRange(modules);
        }

        public void AddWebProviderModules(IEnumerable<ITestWebProvider> modules)
        {
            WebProviders.Clear();
            WebProviders.AddRange(modules);
        }

        public void AddCertificateProviders(IEnumerable<IUserCertificateProvider> modules)
        {
            CertificateProviders.Clear();
            CertificateProviders.AddRange(modules);
        }

        public List<ITestEngineModule> GetTestEngineModules()
        {
            return Modules;
        }

        public List<IUserManager> GetTestEngineUserManager()
        {
            return UserManagers;
        }

        public List<ITestWebProvider> GetTestEngineWebProviders()
        {
            return WebProviders;
        }

        public List<IUserCertificateProvider> GetTestEngineAuthProviders()
        {
            return CertificateProviders;
        }

        public void OnBeforeTestStepExecuted(TestStepEventArgs e)
        {
            BeforeTestStepExecuted?.Invoke(this, e);
        }

        public void OnAfterTestStepExecuted(TestStepEventArgs e)
        {
            AfterTestStepExecuted?.Invoke(this, e);
        }

        public void SetRecordMode()
        {
            _recordMode = true;
        }

        public TestSettings GetTestSettingsFromFile(string filePath, ILogger logger)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _logger = logger;

            try
            {
                if (_fileSystem.FileExists(filePath))
                {
                    var settings = _testConfigParser.ParseTestConfig<TestSettings>(filePath, logger);
                    ProcessPowerFxDefinitions(settings);
                    return settings;
                }
                else
                {
                    logger.LogWarning($"Test settings file not found: {filePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error loading test settings file: {ex.Message}");
                throw;
            }
        }
    }
}
