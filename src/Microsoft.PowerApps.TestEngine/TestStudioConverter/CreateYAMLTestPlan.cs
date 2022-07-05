// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Microsoft.PowerApps.TestEngine.System;
using System.Text.RegularExpressions;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.TestStudioConverter
{
    public static class CreateYAMLTestPlan
    {
        private static string? InputDir;

        private static List<string> TestSteps = new List<string>();

        private static string? TestName;

        private static string? TestDescription;

        private static string[] NoChangeCommands = { "Assert", "Select" };

        public static void exportYAML(string dir)
        {

            InputDir = dir;

            if (!File.Exists(InputDir))
            {
                throw new FileNotFoundException(InputDir + " does not exist");
            }

            readJson(InputDir);

            Console.WriteLine("Test JSON Location: " + InputDir);

            var outputDir = InputDir.Substring(0, InputDir.Length - 4) + "fx.yaml";

            writeYaml(outputDir);

            Console.WriteLine("YAML TestPlan Location: " + outputDir);

        }

        private static void readJson(string InputDir)
        {

            JObject jobj;

            using (var sr = File.OpenText(InputDir))
            using (var reader = new JsonTextReader(sr))
            {
                jobj = (JObject)JToken.ReadFrom(reader);
            }

            var topLevelTestSteps = jobj.Root["TopParent"]["Children"][0]["Children"][0]["Rules"];

            if (!topLevelTestSteps.HasValues)
            {
                return;
            }

            foreach (var x in topLevelTestSteps)
            {
                if (x["Category"].ToString().Equals("Behavior"))
                {
                    var testStep = x["InvariantScript"].ToString();
                    if (!string.IsNullOrEmpty(testStep))
                    {
                        TestSteps.Add(testStep);
                    }
                    else
                    {
                        Console.WriteLine("Empty");
                    }
                }

                if (x["Property"].ToString().Equals("Description"))
                {
                    var description = x["InvariantScript"].ToString();
                    TestDescription = description.Replace("\"", "");
                }

                if (x["Property"].ToString().Equals("DisplayName"))
                {
                    var caseName = x["InvariantScript"].ToString();

                    TestName = caseName.Replace("\"", "");
                }

            }
        }

        private static void writeYaml(string outputDir)
        {

            if (string.IsNullOrEmpty(TestName))
                TestName = "Missing Test Name";

            if (string.IsNullOrEmpty(TestDescription))
                TestDescription = "Missing Test Description";

            var stringBuilder = new StringBuilder("= \n");

            foreach (var step in TestSteps)
            {
                stringBuilder.Append(validateStep(step));
                stringBuilder.Append(";\n");
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

            try
            {
                using (var sw = new StreamWriter(outputDir))
                {
                    serializer.Serialize(sw, testYAML);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        /// <summary>
        /// Checks if a test step needs to be changed to match Test Engine Syntax
        /// </summary>
        /// <param name="step">A Test Step in Test Studio's Power fx syntax</param>
        /// <returns></returns>
        private static string validateStep(string step)
        {
            var name = step.Split('(')[0];

            if (NoChangeCommands.Contains(name))
            {
                return step;
            }
            var parameters = Regex.Match(step, @"\(.*\)").Groups[0].Value;
            parameters = parameters.Substring(1, parameters.Length - 2);

            switch (name)
            {
                case "SetProperty":
                    var property = parameters.Split(",")[0];
                    var value = parameters.Split(",")[1];
                    step = name + "(" + property.Split(".")[0] + "," + "\"" + property.Split(".")[1] + "\"" + "," + value + ")";
                    break;
                default:
                    step = "Assert( true,\"" + step + " is not supported \")";
                    break;
            }

            return step;
        }

    }
}
