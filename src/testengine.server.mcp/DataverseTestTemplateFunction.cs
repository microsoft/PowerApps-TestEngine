// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.MCP
{
    /// <summary>
    /// Provides a PowerFx function to generate Dataverse test templates.
    /// </summary>
    public class DataverseTestTemplateFunction : ReflectionFunction
    {
        private const string FunctionName = "GenerateDataverseTestTemplate";
        private static bool _recommendationAdded = false;

        public DataverseTestTemplateFunction()
            : base(DPath.Root, FunctionName, RecordType.Empty(), RecordType.Empty())
        {
        }

        public RecordValue Execute()
        {
            return ExecuteAsync().Result;
        }

        public async Task<RecordValue> ExecuteAsync()
        {
            // Only return the template once to avoid duplicates
            if (_recommendationAdded)
            {
                return RecordValue.NewRecordFromFields(new[]
                {
                    new NamedValue("Success", BooleanValue.New(true)),
                    new NamedValue("Message", StringValue.New("Template already added"))
                });
            }

            _recommendationAdded = true;

            var template = @"Use the following yaml test template to generate Dataverse Tests
-----------------------
file: entity.te.yaml
-----------------------

# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Dataverse tests
  testSuiteDescription: Validate Power Fx can be used to run Dataverse integration tests
  persona: User1
  appLogicalName: N/A
  onTestCaseStart: |
    = ForAll(Accounts, Remove(Accounts, ThisRecord))

  testCases:
  - testCaseName: No Accounts
    testCaseDescription: Should have no accounts as onTestCaseStart removes all accounts
    testSteps: |
      = Assert(CountRows(Accounts)=0)
  - testCaseName: Insert Account
    testCaseDescription: Insert a new record into account table
    testSteps: |
      = Collect(
          Accounts,
          {
            name: ""New Account""
          }
        );
        Assert(CountRows(Accounts)=1)
  - testCaseName: Insert and Remove Account
    testCaseDescription: Insert a new record into account table and then remove
    testSteps: |
      = Collect(
          Accounts,
          {
            name: ""New Account""
          }
        );
        Assert(CountRows(Accounts)=1);
        Remove(Accounts, First(Accounts));
        Assert(CountRows(Accounts)=0)
  - testCaseName: Update Account
    testCaseDescription: Update created record
    testSteps: |
      =  Collect(
          Accounts,
          {
            name: ""New Account""
          }
        );
        Patch(
          Accounts,
          First(Accounts),
          {
            name: ""Updated Account""
          }
        );
        Assert(First(Accounts).name = ""Updated Account"");
    
  testSettings:
    headless: false
    locale: ""en-US""
    recordVideo: true
    extensionModules:
      enable: true
      parameters:
        enableDataverseFunctions: true
        enableAIFunctions: true
    browserConfigurations:
    - browser: Chromium

  environmentVariables:
  users:
  - personaName: User1
    emailKey: user1Email
    passwordKey: NotNeeded";

            return RecordValue.NewRecordFromFields(new[]
            {
                new NamedValue("Success", BooleanValue.New(true)),
                new NamedValue("Template", StringValue.New(template)),
                new NamedValue("Type", StringValue.New("Yaml Test Template")),
                new NamedValue("Priority", StringValue.New("High"))
            });
        }

        /// <summary>
        /// Resets the function state to allow recommendations again
        /// (primarily used for testing scenarios)
        /// </summary>
        public static void Reset()
        {
            _recommendationAdded = false;
        }
    }
}
