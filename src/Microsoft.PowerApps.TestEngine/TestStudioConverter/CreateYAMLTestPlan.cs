// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Microsoft.PowerApps.TestEngine.System;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.TestStudioConverter
{
    public class CreateYAMLTestPlan
    {
        private readonly ILogger<CreateYAMLTestPlan> _logger;
        private readonly IFileSystem _fileSystem;

        private string? InputDir;

        private static List<string> TestSteps = new List<string>();

        private TestPlanDefinition? YamlTestPlan;

        private static string? TestName;

        private static string? TestDescription;

        private static string[] NoChangeCommands = { "Assert", "Select" };

        const string SetProperty = "SetProperty";

        public CreateYAMLTestPlan(ILogger<CreateYAMLTestPlan> logger, string dir)
        {
            _logger = logger;
            _fileSystem = new FileSystem();
            InputDir = dir;
        }

        public CreateYAMLTestPlan(ILogger<CreateYAMLTestPlan> logger, string dir, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            InputDir = dir;
        }

        public void exportYAML()
        {
            if (!_fileSystem.IsValidFilePath(InputDir))
            {
                throw new DirectoryNotFoundException(InputDir + " is not a valid file path");
            }

            readJson(InputDir);

            _logger.LogInformation($"Test JSON Location: {InputDir}");

            var outputDir = InputDir.Substring(0, InputDir.Length - 4) + "fx.yaml";
            _logger.LogInformation($"YAML TestPlan Location: {outputDir}");
            writeYaml(outputDir);

        }

        private void readJson(string InputDir)
        {
            JObject jobj = JObject.Parse(_fileSystem.ReadAllText(InputDir));
            JToken? topLevelTestSteps = jobj.Root["TopParent"]?["Children"]?[0]?["Children"]?[0]?["Rules"];

            if (topLevelTestSteps == null)
            {
                _logger.LogError("Missing Test Steps");
                return;
            }

            foreach (var topLevelTestStep in topLevelTestSteps)
            {
                if ((topLevelTestStep["Category"]??=false).ToString().Equals("Behavior"))
                {
                    var testStep = topLevelTestStep["InvariantScript"]?.ToString();
                    if (!string.IsNullOrEmpty(testStep))
                    {
                        TestSteps.Add(testStep);
                    }
                    else
                    {
                        _logger.LogWarning("Missing Test Step");
                    }
                }

                if ((topLevelTestStep["Property"]??=false).ToString().Equals("Description"))
                {
                    var description = topLevelTestStep["InvariantScript"]?.ToString();
                    TestDescription = description?.Replace("\"", "");
                }

                if ((topLevelTestStep["Property"]??=false).ToString().Equals("DisplayName"))
                {
                    var caseName = topLevelTestStep["InvariantScript"]?.ToString();

                    TestName = caseName?.Replace("\"", "");
                }

            }
        }

        private void writeYaml(string outputDir)
        {
            if (string.IsNullOrEmpty(TestName))
                TestName = "Missing Test Name";

            if (string.IsNullOrEmpty(TestDescription))
                TestDescription = "Missing Test Description";

            var stringBuilder = new StringBuilder("= \n");

            if (TestSteps.Count == 0)
            {
                _logger.LogWarning("Empty Test Steps");
            }
            else
            {
                foreach (var step in TestSteps)
                {
                    stringBuilder.Append(validateStep(step));
                    stringBuilder.Append(";\n");
                }
            }

            var formattedTestSteps = stringBuilder.ToString();

            var testYAML = new TestPlanDefinition
            {
                Test = new ConverterTestDefinition
                {
                    Name = TestName,
                    Description = TestDescription,
                    Persona = "User1",
                    AppLogicalName = "Replace with appLogicalName",
                    TestSteps = formattedTestSteps,
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
            YamlTestPlan = testYAML;
            _fileSystem.WriteTextToFile(outputDir, result);
        }
        /// <summary>
        /// Checks if a test step needs to be changed to match Test Engine Syntax
        /// </summary>
        /// <param name="step">A Test Step in Test Studio's Power fx syntax</param>
        /// <returns></returns>
        private string validateStep(string step)
        {
            var identifier = step.Split('(')[0];

            if (NoChangeCommands.Contains(identifier))
            {
                return step;
            }
            var parameters = Regex.Match(step, @"\(.*\)").Groups[0].Value;
            parameters = parameters.Substring(1, parameters.Length - 2);

            switch (identifier)
            {
                case SetProperty:
                    // Expected Test Studio syntax sample:  SetProperty(IncrementControl1.value, 10)
                    // Resulting Test Engine syntax sample: SetProperty(IncrementControl1, "value", 10)
                    
                    var parametersSplit = parameters.Split(new[] {','}, 2);

                    if(parametersSplit.Length < 2)
                    {
                        step = "Assert( true,\"" + step + " incorrect Test Studio syntax \")";
                        _logger.LogWarning($"{step} incorrect syntax");
                        break;
                    }

                    var property = parametersSplit[0];
                    var value = parametersSplit[1];

                    var propertySplitArray = property.Split(".");

                    if (propertySplitArray.Length > 1)
                    {
                        step = identifier + "(" + propertySplitArray[0] + "," + "\"" + propertySplitArray[1] + "\"" + "," + value + ")";
                        break;
                    }
                    else
                    {
                        step = "Assert( true,\"" + step + " missing property \")";
                        _logger.LogWarning($"{step} has a missing property");
                        break;
                    }

                default:
                    step = "Assert( true,\"" + step + " is not supported \")";
                    _logger.LogWarning($"{step} is not supported");
                    break;
            }

            return step;
        }

        public List<string> GetTestSteps()
        {
            return TestSteps;
        }

        public TestPlanDefinition? GetYamlTestPlan()
        {
            return YamlTestPlan;
        }
    }
}
