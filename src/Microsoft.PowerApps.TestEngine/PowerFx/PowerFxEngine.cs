// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Design;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Dataverse;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Microsoft.PowerApps.TestEngine.PowerFx
{
    /// <summary>
    /// Wrapper for Power FX interpreter
    /// </summary>
    public class PowerFxEngine : IPowerFxEngine
    {
        public const string ENABLE_DATAVERSE_FUNCTIONS = "enableDataverseFunctions";
        public const string ENABLE_AI_FUNCTIONS = "enableAIFunctions";

        private readonly ITestInfraFunctions TestInfraFunctions;
        private readonly ITestWebProvider _testWebProvider;
        private readonly IFileSystem _fileSystem;
        private readonly ISingleTestInstanceState SingleTestInstanceState;
        private readonly ITestState TestState;
        private readonly IEnvironmentVariable _environmentVariable;
        private IOrganizationService _orgService;
        private DataverseConnection _dataverseConnection;
        private DataverseAIPredictHelper _dataverseAIPredictHelper;
        private bool enableAIFunctions = false;


        private int _retryLimit = 2;

        public RecalcEngine Engine { get; private set; }
        private ILogger Logger { get { return SingleTestInstanceState.GetLogger(); } }

        public Func<AzureCliHelper> GetAzureCliHelper = () => new AzureCliHelper();

        public PowerFxEngine(ITestInfraFunctions testInfraFunctions,
                             ITestWebProvider testWebProvider,
                             ISingleTestInstanceState singleTestInstanceState,
                             ITestState testState,
                             IFileSystem fileSystem,
                             IEnvironmentVariable environmentVariable)
        {
            TestInfraFunctions = testInfraFunctions;
            _testWebProvider = testWebProvider;
            SingleTestInstanceState = singleTestInstanceState;
            TestState = testState;
            _fileSystem = fileSystem;
            _environmentVariable = environmentVariable;
        }

        /// <summary>
        /// Setup the Power Fx state for test execution
        /// </summary>
        public void Setup(TestSettings settings)
        {
            var features = Features.PowerFxV1;

            var powerFxConfig = new PowerFxConfig(features);

            var vals = new SymbolValues();
            var symbols = (SymbolTable)vals.SymbolTable;
            symbols.EnableMutationFunctions();

            var testSettings = TestState.GetTestSettings();

            powerFxConfig.SymbolTable = symbols;

            ConditionallyRegisterTestTypes(testSettings, powerFxConfig);

            // Enabled to allow ability to set variable and collection state that can be used with providers and as test variables
            powerFxConfig.EnableSetFunction();
            powerFxConfig.EnableJsonFunctions();

            // Perform any provider specific configuration
            if (_testWebProvider is IExtendedPowerFxProvider extendedPowerFxProvider)
            {
                extendedPowerFxProvider.ConfigurePowerFx(powerFxConfig);
            }

            powerFxConfig.AddFunction(new SelectOneParamFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectTwoParamsFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectThreeParamsFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new SelectFileTwoParamsFunction(_testWebProvider, async () => await UpdatePowerFxModelAsync(), Logger));
            powerFxConfig.AddFunction(new ScreenshotFunction(TestInfraFunctions, SingleTestInstanceState, _fileSystem, Logger));
            powerFxConfig.AddFunction(new AssertWithoutMessageFunction(Logger));
            powerFxConfig.AddFunction(new AssertFunction(Logger));
            powerFxConfig.AddFunction(new AssertNotErrorFunction(Logger));
            powerFxConfig.AddFunction(new SetPropertyFunction(_testWebProvider, Logger));
            powerFxConfig.AddFunction(new NavigateToRecordFunction(_testWebProvider, this, Logger));
            powerFxConfig.AddFunction(new SetDOBFieldsFunction(_testWebProvider, Logger));
            powerFxConfig.AddFunction(new SelectGridRowCheckboxFunction(_testWebProvider, Logger));

            powerFxConfig.AddFunction(new IsMatchFunction(Logger));

            if (settings != null && settings.ExtensionModules != null && settings.ExtensionModules.Enable)
            {
                var modules = TestState.GetTestEngineModules();
                if (modules.Count == 0)
                {
                    Logger.LogError("Extension enabled, none loaded");
                }
                foreach (var module in modules)
                {
                    module.RegisterPowerFxFunction(powerFxConfig, TestInfraFunctions, _testWebProvider, SingleTestInstanceState, TestState, _fileSystem);
                }
            }
            else
            {
                if (TestState.GetTestEngineModules().Count > 0)
                {
                    Logger.LogInformation("Extension loaded but not enabled");
                }
            }

            WaitRegisterExtensions.RegisterAll(powerFxConfig, TestState.GetTimeout(), Logger);

            var provider = TestState.TestProvider;
            if (provider is IExtendedPowerFxProvider powerFxProvider)
            {
                powerFxProvider.Setup(powerFxConfig, TestInfraFunctions, SingleTestInstanceState, TestState, _fileSystem);
            }

            Engine = new RecalcEngine(powerFxConfig);

            // Add any provider specific functions or state
            if (_testWebProvider is IExtendedPowerFxProvider extendedProviderAfter)
            {
                extendedProviderAfter.ConfigurePowerFxEngine(Engine);
            }

            ConditionallySetupDataverse(testSettings, powerFxConfig);
            ConditionallyRegisterTestFunctions(testSettings, powerFxConfig);

            var symbolValues = new SymbolValues(powerFxConfig.SymbolTable);

            foreach (var val in powerFxConfig.SymbolTable.SymbolNames.ToList())
            {
                if (powerFxConfig.SymbolTable.TryLookupSlot(val.Name, out ISymbolSlot slot))
                {
                    Engine.UpdateVariable(val.Name, symbolValues.Get(slot));
                    powerFxConfig.SymbolTable.RemoveVariable(val.Name);
                }
            }
        }

        /// <summary>
        /// Register Power Fx types that aid and simplify testing
        /// </summary>
        /// <param name="testSettings">The settings to obtain the test functions from</param>
        /// <param name="powerFxConfig">The Power Fx context that the functions should be registered with</param>
        private void ConditionallyRegisterTestTypes(TestSettings testSettings, PowerFxConfig powerFxConfig)
        {
            if (testSettings == null || testSettings.PowerFxTestTypes == null || testSettings.PowerFxTestTypes.Count == 0)
            {
                return;
            }

            var engine = new RecalcEngine(new PowerFxConfig(Features.PowerFxV1));

            foreach (PowerFxTestType type in testSettings.PowerFxTestTypes)
            {
                var result = engine.Parse(type.Value);
                RegisterPowerFxType(type.Name, result.Root, powerFxConfig);
            }
        }

        private void RegisterPowerFxType(string name, TexlNode result, PowerFxConfig powerFxConfig)
        {
            switch (result.Kind)
            {
                case NodeKind.Table:
                    var table = TableType.Empty();
                    var tableRecord = RecordType.Empty();
                    var first = true;

                    TableNode tableNode = result as TableNode;

                    foreach (var child in tableNode.ChildNodes)
                    {
                        if (child is RecordNode recordNode && first)
                        {
                            first = false;
                            tableRecord = GetRecordType(recordNode);

                            foreach (var field in tableRecord.GetFieldTypes())
                            {
                                table = table.Add(field);
                            }
                        }
                    }

                    powerFxConfig.SymbolTable.AddType(new DName(name), table);
                    break;
                case NodeKind.Record:
                    var record = GetRecordType(result as RecordNode);

                    powerFxConfig.SymbolTable.AddType(new DName(name), record);
                    break;
            }
        }

        private RecordType GetRecordType(RecordNode recordNode)
        {
            var record = RecordType.Empty();
            int index = 0;
            foreach (var child in recordNode.ChildNodes)
            {
                if (child is DottedNameNode dottedNameNode)
                {
                    var fieldName = dottedNameNode.Right.Name.Value;
                    var fieldType = GetFormulaTypeFromNode(dottedNameNode.Right);
                    record = record.Add(new NamedFormulaType(fieldName, fieldType));
                }
                if (child is FirstNameNode firstNameNode)
                {
                    var fieldName = recordNode.Ids[index].Name.Value;
                    index++;
                    var fieldType = GetFormulaTypeFromNode(firstNameNode.Ident);
                    record = record.Add(new NamedFormulaType(fieldName, fieldType));
                }
            }
            return record;
        }

        private FormulaType GetFormulaTypeFromNode(Identifier right)
        {
            switch (right.Name.Value)
            {
                case "Boolean":
                    return FormulaType.Boolean;
                case "Number":
                    return FormulaType.Number;
                case "Text":
                    return FormulaType.String;
                case "Date":
                    return FormulaType.Date;
                case "DateTime":
                    return FormulaType.DateTime;
                case "Time":
                    return FormulaType.Time;
                default:
                    throw new InvalidOperationException($"Unsupported node type: {right.Name.Value}");
            }
        }

        /// <summary>
        /// Register Power Fx funtions that aid and simplify testing
        /// </summary>
        /// <param name="testSettings">The settings to obtain the test functions from</param>
        /// <param name="powerFxConfig">The Power Fx context that the functions should be registered with</param>
        private void ConditionallyRegisterTestFunctions(TestSettings testSettings, PowerFxConfig powerFxConfig)
        {
            if (testSettings == null)
            {
                return;
            }

            if (testSettings.TestFunctions.Count > 0)
            {
                var culture = GetLocaleFromTestSettings(testSettings.Locale);

                foreach (var function in testSettings.TestFunctions)
                {
                    var code = function.Code.TrimEnd();
                    if (!code.EndsWith(";"))
                    {
                        code += ";";
                    }
                    var registerResult = Engine.AddUserDefinedFunction(code, culture, powerFxConfig.SymbolTable, true);
                    if (!registerResult.IsSuccess)
                    {
                        foreach (var error in registerResult.Errors)
                        {
                            var msg = error.ToString();

                            if (error.IsWarning)
                            {
                                Logger.LogWarning(msg);
                            }
                            else
                            {
                                Logger.LogError(msg);
                            }
                        }
                    }
                }
            }
        }

        private CultureInfo GetLocaleFromTestSettings(string strLocale)
        {
            var locale = CultureInfo.CurrentCulture;
            try
            {
                if (string.IsNullOrEmpty(strLocale))
                {
                    Logger.LogDebug($"Locale property not specified in testSettings. Using current system locale: {locale.Name}");
                }
                else
                {
                    locale = new CultureInfo(strLocale);
                    Logger.LogDebug($"Locale: {locale.Name}");
                }
                return locale;
            }
            catch (CultureNotFoundException)
            {
                Logger.LogError($"Locale from test suite definition {strLocale} unrecognized.");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings.ToString());
            }
        }

        /// <summary>
        /// Attach dataverse state to the test session if we are testing a Model Driven Application or have a dataverse URL we are testing
        /// </summary>
        private void ConditionallySetupDataverse(TestSettings testSettings, PowerFxConfig powerFxConfig)
        {
            if (testSettings == null || !testSettings.ExtensionModules.Parameters.ContainsKey(ENABLE_DATAVERSE_FUNCTIONS) || (testSettings != null && testSettings.ExtensionModules.Parameters[ENABLE_DATAVERSE_FUNCTIONS].ToString().ToLower() != "true"))
            {
                return;
            }

            // Must have dataverse enabled to enable AI Functions
            if (testSettings != null && testSettings.ExtensionModules.Parameters.ContainsKey(ENABLE_AI_FUNCTIONS) && testSettings.ExtensionModules.Parameters[ENABLE_AI_FUNCTIONS].ToString().ToLower() == "true")
            {
                enableAIFunctions = true;
            }

            DataverseConnection dataverse = null;

            var domainUrl = TestState.GetDomain();

            if (domainUrl.Contains("dynamics.com"))
            {
                domainUrl = "https://" + new Uri(domainUrl).Host;
            }
            else
            {
                domainUrl = "";
            }

            // Fallback to environment to check if a DATAVERSE_URL value has been configured.
            var dataverseUrl = !string.IsNullOrEmpty(domainUrl) ? domainUrl : _environmentVariable.GetVariable("DATAVERSE_URL");

            if (!string.IsNullOrEmpty(dataverseUrl) && Uri.TryCreate(dataverseUrl, UriKind.Absolute, out Uri dataverseUri))
            {
                // Attempt to retreive OAuath access token. Assume logged in Azure CLI session
                string token = GetAzureCliHelper()?.GetAccessToken(dataverseUri);
                if (!string.IsNullOrEmpty(token))
                {
                    // Establish a collection to Dataverse
                    Logger.LogInformation($"Loading dataverse state for {dataverseUri}");
                    Func<string, Task<string>> callback = async (string item) =>
                    {
                        return token;
                    };
                    var svcClient = new ServiceClient(dataverseUri, callback) { UseWebApi = false };
                    svcClient.Connect();

                    _orgService = svcClient;

                    dataverse = SingleOrgPolicy.New(svcClient);

                    _dataverseConnection = dataverse;


                    if (enableAIFunctions)
                    {

                        _dataverseAIPredictHelper = new DataverseAIPredictHelper(dataverseUri, token);
#if DEBUG
                        powerFxConfig.AddFunction(new AIExecutePromptFunction(Logger, svcClient, _dataverseAIPredictHelper));
#endif
                    }

                }
            }
        }

        public async Task<FormulaValue> ExecuteWithRetryAsync(string testSteps, CultureInfo culture)
        {
            int currentRetry = 0;
            FormulaValue result = FormulaValue.NewBlank();

            while (currentRetry <= _retryLimit)
            {
                try
                {
                    result = await ExecuteAsync(testSteps, culture);
                    break;
                }
                catch (Exception e) when (e.Message.Contains("locale"))
                {
                    Logger.LogDebug($"Got {e.Message} in attempt No.{currentRetry + 1} to run");
                    currentRetry++;
                    if (currentRetry > _retryLimit)
                    {
                        // Re-throw the exception. 
                        throw;
                    }

                    // Wait to retry the operation.
                    Thread.Sleep(1000);
                    await UpdatePowerFxModelAsync();
                }
            }
            return result;
        }

        public async Task<FormulaValue> ExecuteAsync(string testSteps, CultureInfo culture)
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException();
            }

            // Remove the leading = sign
            if (testSteps.StartsWith("="))
            {
                testSteps = testSteps.Remove(0, 1);
            }

            var goStepByStep = TestState.ExecuteStepByStep;

            var parseResult = Engine.Parse(testSteps);

            // Check if the syntax is correct
            var checkResult = Engine.Check(testSteps, null, GetPowerFxParserOptions(culture));
            if (!checkResult.IsSuccess)
            {
                // If it isn't, we have to go step by step as the object model isn't fully loaded
                goStepByStep = true;
                Logger.LogDebug($"Syntax check failed. Now attempting to execute lines step by step");
            }

            var runtimeConfig = new RuntimeConfig();

            // Check if a connection to Dataverse has been established
            if (_orgService != null)
            {
                // Add in symbols from Dataverse
                runtimeConfig = new RuntimeConfig(_dataverseConnection.SymbolValues);

                // And enable dataverse execution as part of this test session
                runtimeConfig.AddDataverseExecute(_orgService);
            }

            if (TestState.TestProvider is IExtendedPowerFxProvider extendedProvider && extendedProvider.ProviderExecute)
            {
                return extendedProvider.ExecutePowerFx(testSteps, culture);
            }

            var parseOption = new ParserOptions() { AllowsSideEffects = true, Culture = culture, NumberIsFloat = true };

            if (goStepByStep)
            {
                var splitSteps = PowerFxHelper.ExtractFormulasSeparatedByChainingOperator(Engine, checkResult, culture);
                FormulaValue result = FormulaValue.NewBlank();

                int stepNumber = 0;

                foreach (var step in splitSteps)
                {
                    TestState.OnBeforeTestStepExecuted(new TestStepEventArgs { TestStep = step, StepNumber = stepNumber, Engine = Engine });

                    Logger.LogTrace($"Attempting:{step.Replace("\n", "").Replace("\r", "")}");

                    result = await Engine.EvalAsync(
                                      step
                                    , CancellationToken.None
                                    , parseOption, null, runtimeConfig);

                    TestState.OnAfterTestStepExecuted(new TestStepEventArgs { TestStep = step, Result = result, StepNumber = stepNumber, Engine = Engine });
                    stepNumber++;
                }
                return result;
            }
            else
            {
                var values = new SymbolValues();
                Logger.LogTrace($"Attempting:\n\n{{\n{testSteps}}}");
                TestState.OnBeforeTestStepExecuted(new TestStepEventArgs { TestStep = testSteps, StepNumber = null, Engine = Engine });

                var result = await Engine.EvalAsync(
                                      testSteps
                                    , CancellationToken.None
                                    , parseOption, null, runtimeConfig);

                TestState.OnAfterTestStepExecuted(new TestStepEventArgs { TestStep = testSteps, Result = result, StepNumber = 1 });
                return result;
            }
        }


        public async Task UpdatePowerFxModelAsync()
        {
            if (Engine == null)
            {
                Logger.LogError("Engine is null, make sure to call Setup first");
                throw new InvalidOperationException();
            }

            await PollingHelper.PollAsync<bool>(false, (x) => !x, () => _testWebProvider.CheckIsIdleAsync(), TestState.GetTestSettings().Timeout, SingleTestInstanceState.GetLogger(), "Something went wrong when Test Engine tried to get App status.");

            var controlRecordValues = await _testWebProvider.LoadObjectModelAsync();
            foreach (var control in controlRecordValues)
            {
                Engine.UpdateVariable(control.Key, control.Value);
            }
        }

        private static ParserOptions GetPowerFxParserOptions(CultureInfo culture)
        {
            // Currently support for decimal is in progress for PowerApps
            // Power Fx by default treats number as decimal. Hence setting NumberIsFloat config to true in our case

            // TODO: Evuate culture evaluate across languages
            return new ParserOptions() { AllowsSideEffects = true, Culture = culture, NumberIsFloat = true };
        }

        public ITestWebProvider GetWebProvider()
        {
            return _testWebProvider;
        }

        public async Task RunRequirementsCheckAsync()
        {
            await _testWebProvider.CheckProviderAsync();
            await _testWebProvider.TestEngineReady();
        }

        public bool PowerAppIntegrationEnabled { get; set; } = true;
    }
}
