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


namespace Microsoft.PowerApps.TestEngine.Helpers
{
    public static class CreateYAMLTestPlan
    {
        private static string? InputDir;

        private static List<String> TestSteps = new List<String>();

        private static string? TestName;

        private static string? TestDescription;

        class TestYAML
        {
            public TestDefinition test { get; set; }
            public TestSettings testSettings { get; set; }
            public EnvironmentVariables environmentVariables { get; set; }

        }

        class TestDefinition
        {
            public string name { get; set; }
            public string description { get; set; }

            public string persona { get; set; }

            public string appLogicalName { get; set; }
            [YamlMember(ScalarStyle = ScalarStyle.Literal)]
            public string testSteps { get; set; }


        }

        class TestSettings
        {
            public bool recordVideo { get; set; }
            public List<BrowserConfiguration> browserConfigurations { get; set; }

        }

        class BrowserConfiguration
        {
            public string browser { get; set; }
        }


        class EnvironmentVariables
        {
            public List<user> users { get; set; }
        }

        class user
        {
            public string personaName { get; set; }
            public string emailKey { get; set; }
            public string passwordKey { get; set; }

        }

        public static void exportYAML(string dir)
        {

            InputDir = dir;

            if (!File.Exists(InputDir))
            {
                throw new FileNotFoundException(InputDir + " does not exist");
            }

            readJson(InputDir);

            Console.WriteLine("Test JSON Location: " + InputDir);

            string outputDir = InputDir.Substring(0, InputDir.Length - 4) + "fx.yaml";

            writeYaml(outputDir);

            Console.WriteLine("YAML TestPlan Location: " + outputDir);

        }

        private static void readJson(string InputDir)
        {

            JObject jobj;

            using (StreamReader sr = File.OpenText(InputDir))
            using (JsonTextReader reader = new JsonTextReader(sr))
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
                    string testStep = x["InvariantScript"].ToString();
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
                    string description = x["InvariantScript"].ToString();
                    TestDescription = description.Replace("\"", "");
                }

                if (x["Property"].ToString().Equals("DisplayName"))
                {
                    string caseName = x["InvariantScript"].ToString();

                    TestName = caseName.Replace("\"", "");
                }

            }
        }

        private static void writeYaml(string outputDir)
        {

            if (String.IsNullOrEmpty(TestName))
                TestName = "Missing Test Name";

            if (String.IsNullOrEmpty(TestDescription))
                TestDescription = "Missing Test Description";

            StringBuilder stringBuilder = new StringBuilder("= \n");

            foreach (string step in TestSteps)
            {
                stringBuilder.Append(validateStep(step));
                stringBuilder.Append(";\n");
            }

            string formattedTestSteps = stringBuilder.ToString();

            var testYAML = new TestYAML
            {
                test = new TestDefinition
                {
                    name = TestName,
                    description = TestDescription,
                    persona = "User1",
                    appLogicalName = "appLogicalName",

                    testSteps = formattedTestSteps, 

                },
                testSettings = new TestSettings
                {
                    recordVideo = true,
                    browserConfigurations = new List<BrowserConfiguration>(new BrowserConfiguration[] { new BrowserConfiguration { browser = "Edge" } })

                },

                environmentVariables = new EnvironmentVariables
                {
                    users = new List<user>(new user[] { new user { personaName = "User1", emailKey = "user1Email", passwordKey = "user1Password" } })
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
            string name = step.Split('(')[0];

            Console.WriteLine(name);

            if (name == null)
            {
                return step;
            }
            string parameters = step.Split('(')[1];


            return step;
        }

    }
}
