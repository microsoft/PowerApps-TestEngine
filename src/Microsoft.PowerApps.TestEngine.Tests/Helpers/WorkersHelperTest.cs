// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerApps.TestEngine.Config;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
  
    public class WorkersHelperTest
    {
        string testRunId = Guid.NewGuid().ToString();
        string testRunDirectory = Path.Combine("TestOutput", Guid.NewGuid().ToString().Substring(0, 6));
        TestSettings testSettings = new TestSettings()
        {
            Workers = 2,
            BrowserConfigurations = new List<BrowserConfiguration>()
                {
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium"
                    },
                    new BrowserConfiguration()
                    {
                        Browser = "Firefox"
                    },
                    new BrowserConfiguration()
                    {
                        Browser = "Chromium",
                        Device = "Pixel 2"
                    }
                }
        };
        List<TestDefinition> testDefinitions = new List<TestDefinition>()
        {
            new TestDefinition()
            {
                Name = "Test1",
                Description = "First test",
                AppLogicalName = "logicalAppName1",
                Persona = "User1",
                TestSteps = "Assert(1 + 1 = 2, \"1 + 1 should be 2 \")"
            },
            new TestDefinition()
            {
                Name = "Test2",
                Description = "Second test",
                AppLogicalName = "logicalAppName2",
                Persona = "User2",
                TestSteps = "Assert(2 + 1 = 3, \"2 + 1 should be 3 \")"
            }
        };

        [Fact]
        public void TotalTestRunSuccessTest()
        {
            WorkersHelper workersHelper = new WorkersHelper();
            Assert.Equal(workersHelper.TotalTestRun(testRunId, testRunDirectory, testDefinitions, testSettings), 3);
        }

        [Fact]
        public void TotalTestRunFailureTest()
        {
            WorkersHelper workersHelper = new WorkersHelper();
            Assert.NotEqual(workersHelper.TotalTestRun(testRunId, testRunDirectory, testDefinitions, testSettings), 4);
        }

    }
}