// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.PowerApps.TestEngine.TestStudioConverter
{
    public class CreateYamlTestPlan
    {
        private readonly ILogger<CreateYamlTestPlan> _logger;
        private readonly IFileSystem _fileSystem;

        private string InputDir;

        public List<TestCase> TestCases = new List<TestCase>();

        private TestPlanDefinition? YamlTestPlan;

        private static string? YamlTestPlanString;

        private static string? TestSuiteName;

        private static string? TestSuiteDescription;

        private static string? OnTestCaseStart;

        private static string? OnTestCaseComplete;

        private static string? OnTestSuiteComplete;

        const string SetProperty = "SetProperty";

        public CreateYamlTestPlan(ILogger<CreateYamlTestPlan> logger, string dir)
        {
            _logger = logger;
            _fileSystem = new FileSystem();
            InputDir = dir;
        }

        public CreateYamlTestPlan(ILogger<CreateYamlTestPlan> logger, string dir, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            InputDir = dir;
        }

        public void ExportYaml()
        {
            if (!_fileSystem.IsValidFilePath(InputDir))
            {
                _logger.LogTrace($"File path: {InputDir}");
                _logger.LogError("Found invalid file path.");
                throw new DirectoryNotFoundException();
            }

            ReadJson(InputDir);

            _logger.LogInformation($"Test JSON Location: {InputDir}");

            var outputDir = InputDir.Substring(0, InputDir.Length - 4) + "fx.yaml";
            _logger.LogInformation($"YAML TestPlan Location: {outputDir}");
            WriteYaml(outputDir);

        }

        private void ReadJson(string InputDir)
        {
            JObject jobj = JObject.Parse(_fileSystem.ReadAllText(InputDir));

            // Read test suite information
            JToken testSuiteProperties = jobj.Root["TopParent"]["Children"][0]["Rules"];
            if (testSuiteProperties == null || testSuiteProperties.Count() == 0)
            {
                _logger.LogError("Missing Test Suite Information");
                return;
            }

            foreach (var testSuiteProperty in testSuiteProperties)
            {
                if ((testSuiteProperty["Property"] ??= false).ToString().Equals("Description"))
                {
                    TestSuiteDescription = ExtractScripts(testSuiteProperty);
                }

                if ((testSuiteProperty["Property"] ??= false).ToString().Equals("DisplayName"))
                {
                    TestSuiteName = ExtractScripts(testSuiteProperty);
                }
            }

            // Read test cases
            JToken testCaseList = jobj.Root["TopParent"]["Children"][0]["Children"];
            if (testCaseList == null || testCaseList.Count() == 0)
            {
                _logger.LogError("Missing Test Cases");
                return;
            }

            foreach (var testCase in testCaseList)
            {
                TestCase testCaseObj = new TestCase();
                List<string> testSteps = new List<string>();
                var testCaseProperties = testCase["Rules"];

                if (testCaseProperties == null || testCaseProperties.Count() == 0)
                {
                    _logger.LogError("Missing Test Case Information");
                    return;
                }

                foreach (var testCaseProperty in testCaseProperties)
                {
                    if ((testCaseProperty["Category"] ??= false).ToString().Equals("Behavior"))
                    {
                        var testStep = testCaseProperty["InvariantScript"]?.ToString();
                        if (!string.IsNullOrEmpty(testStep))
                        {
                            testSteps.Add(testStep);
                        }
                        else
                        {
                            _logger.LogWarning("Missing Test Step");
                        }
                    }

                    if ((testCaseProperty["Property"] ??= false).ToString().Equals("Description"))
                    {
                        testCaseObj.TestCaseDescription = ExtractScripts(testCaseProperty);
                    }

                    if ((testCaseProperty["Property"] ??= false).ToString().Equals("DisplayName"))
                    {
                        testCaseObj.TestCaseName = ExtractScripts(testCaseProperty);
                    }
                }

                testCaseObj.TestSteps = CombineTestSteps(testSteps);
                TestCases.Add(testCaseObj);
            }

            // Read OnTestCaseStart, OnTestCaseComplete, OnTestSuiteComplete
            JToken overallProperties = jobj.Root["TopParent"]["Rules"];
            if (overallProperties != null && overallProperties.Count() > 0)
            {
                foreach (var overallProperty in overallProperties)
                {
                    if ((overallProperty["Property"] ??= false).ToString().Equals("OnTestStart"))
                    {
                        OnTestCaseStart = ExtractScripts(overallProperty, true);
                    }

                    if ((overallProperty["Property"] ??= false).ToString().Equals("OnTestComplete"))
                    {
                        OnTestCaseComplete = ExtractScripts(overallProperty, true);
                    }

                    if ((overallProperty["Property"] ??= false).ToString().Equals("OnTestSuiteComplete"))
                    {
                        OnTestSuiteComplete = ExtractScripts(overallProperty, true);
                    }
                }
            }
        }

        private string ExtractScripts(JToken input, bool convertToList = false)
        {
            if (input == null)
            {
                return "";
            }

            var script = input["InvariantScript"]?.ToString();

            if (string.IsNullOrEmpty(script))
            {
                return "";
            }
            else if (convertToList)
            {
                return CombineTestSteps(script.Split(";\r\n").ToList());
            }
            else
            {
                return script.Replace("\"", "");
            }
        }

        private string CombineTestSteps(List<string> testSteps)
        {
            if (testSteps.Count == 0)
            {
                _logger.LogWarning("Empty Test Steps");
                return "";
            }

            var stringBuilder = new StringBuilder("= \n");
            foreach (var step in testSteps)
            {
                stringBuilder.Append(ValidateStep(step));
                stringBuilder.Append(";\n");
            }

            return stringBuilder.ToString();
        }

        private void WriteYaml(string outputDir)
        {
            if (string.IsNullOrEmpty(TestSuiteName))
                TestSuiteName = "Missing Test Name";

            if (string.IsNullOrEmpty(TestSuiteDescription))
                TestSuiteDescription = "Missing Test Description";

            var testYAML = new TestPlanDefinition
            {
                TestSuite = new TestSuiteDefinition
                {
                    TestSuiteName = TestSuiteName,
                    TestSuiteDescription = TestSuiteDescription,
                    Persona = "User1",
                    OnTestCaseStart = OnTestCaseStart,
                    OnTestCaseComplete = OnTestCaseComplete,
                    OnTestSuiteComplete = OnTestSuiteComplete,
                    AppLogicalName = "Replace with appLogicalName",
                    TestCases = TestCases
                },
                TestSettings = new TestSettings
                {
                    RecordVideo = true,
                    BrowserConfigurations = new List<BrowserConfiguration>(new BrowserConfiguration[] { new BrowserConfiguration { Browser = "Chromium" } })
                },
                EnvironmentVariables = new EnvironmentVariables
                {
                    Users = new List<UserConfiguration>(new UserConfiguration[] { new UserConfiguration { PersonaName = "User1", EmailKey = "user1Email", PasswordKey = "user1Password" } })
                }

            };

            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var result = serializer.Serialize(testYAML);
            YamlTestPlanString = result;
            YamlTestPlan = testYAML;
            _fileSystem.WriteTextToFile(outputDir, result);
        }
        /// <summary>
        /// Checks if a test step needs to be changed to match Test Engine Syntax
        /// Right now, ValidateStep only affects the SetProperty Command
        /// </summary>
        /// <param name="step">A Test Step in Test Studio's Power fx syntax</param>
        /// <returns></returns>
        private string ValidateStep(string step)
        {
            var identifier = step.Split('(')[0];

            if (!identifier.Equals(SetProperty))
            {
                return step;
            }
            else
            {
                var parameters = Regex.Match(step, @"\(.*\)").Groups[0].Value;
                parameters = parameters.Substring(1, parameters.Length - 2);

                // Expected Test Studio syntax sample:  SetProperty(IncrementControl1.value, 10)
                // Resulting Test Engine syntax sample: SetProperty(IncrementControl1, "value", 10)

                var parametersSplit = parameters.Split(new[] { ',' }, 2);

                if (parametersSplit.Length < 2)
                {
                    step = "Assert( true,\"" + step + " incorrect Test Studio syntax \")";
                    _logger.LogWarning($"{step} incorrect syntax");
                    return step;
                }

                var property = parametersSplit[0];
                var value = parametersSplit[1];

                var propertySplitArray = property.Split(".");

                if (propertySplitArray.Length > 1)
                {
                    step = identifier + "(" + propertySplitArray[0] + "," + "\"" + propertySplitArray[1] + "\"" + "," + value + ")";
                    return step;
                }
                else
                {
                    step = "Assert( true,\"" + step + " missing property \")";
                    _logger.LogWarning($"{step} has a missing property");
                    return step;
                }
            }
        }

        public List<TestCase> GetTestCases()
        {
            return TestCases;
        }

        public TestPlanDefinition? GetYamlTestPlan()
        {
            return YamlTestPlan;
        }

        public string? GetTestPlanString()
        {
            return YamlTestPlanString;
        }
    }
}
