// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.Reporting.Format;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.Reporting
{
    /// <summary>
    /// Tests for the GroupTestsByRun method in TestRunSummary
    /// </summary>
    public class GroupTestsByRunTests
    {
        private Mock<IFileSystem> MockFileSystem;

        public GroupTestsByRunTests()
        {
            MockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        }        /// <summary>
        /// Helper method to create a test run with a custom app URL
        /// </summary>
        private TestRun CreateTestRunWithAppUrl(string testName, string appUrl, bool passed = true)
        {
            var testRun = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = testName,
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = testName,
                            Outcome = passed ? TestReporter.PassedResultOutcome : TestReporter.FailedResultOutcome,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now.AddSeconds(10),
                            Duration = "00:00:10",
                            Output = new TestOutput
                            {
                                StdOut = "Test output"
                            }
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput
                    {
                        StdOut = $"{{ \"AppURL\": \"{appUrl}\" }}"
                    }
                }
            };
            return testRun;
        }

        [Fact]
        public void GroupTestsByRun_ModelDrivenAppEntityList_GroupsByEntityName()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>
            {
                CreateTestRunWithAppUrl("Account List Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account"),
                CreateTestRunWithAppUrl("Another Account List Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account"),
                CreateTestRunWithAppUrl("Contact List Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=contact")
            };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(2, result.Count); // Should have 2 groups: account and contact
            Assert.True(result.ContainsKey("account"));
            Assert.True(result.ContainsKey("contact"));
            Assert.Equal(2, result["account"].Count); // Two account tests
            Assert.Single(result["contact"]); // One contact test
        }

        [Fact]
        public void GroupTestsByRun_ModelDrivenAppEntityRecords_GroupsByEntityName()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>
            {
                CreateTestRunWithAppUrl("Account Record Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entity&etn=account&id=123"),
                CreateTestRunWithAppUrl("Another Account Record Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entity&etn=account&id=456"),
                CreateTestRunWithAppUrl("Contact Record Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entity&etn=contact&id=789")
            };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(2, result.Count); // Should have 2 groups: account and contact
            Assert.True(result.ContainsKey("account"));
            Assert.True(result.ContainsKey("contact"));
            Assert.Equal(2, result["account"].Count); // Two account tests
            Assert.Single(result["contact"]); // One contact test
        }

        [Fact]
        public void GroupTestsByRun_ModelDrivenAppCustomPageWithName_GroupsByPageName()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>
            {
                CreateTestRunWithAppUrl("Dashboard Custom Page", "https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=dashboard"),
                CreateTestRunWithAppUrl("Another Dashboard Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=dashboard"),
                CreateTestRunWithAppUrl("Settings Custom Page", "https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=settings")
            };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(2, result.Count); // Should have 2 groups: dashboard and settings
            Assert.True(result.ContainsKey("dashboard"));
            Assert.True(result.ContainsKey("settings"));
            Assert.Equal(2, result["dashboard"].Count); // Two dashboard tests
            Assert.Single(result["settings"]); // One settings test
        }

        [Fact]
        public void GroupTestsByRun_ModelDrivenAppCustomPageType_GroupsByPageType()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>
            {
                CreateTestRunWithAppUrl("Dashboard Page Type", "https://contoso.crm.dynamics.com/main.aspx?pagetype=dashboard"),
                CreateTestRunWithAppUrl("Another Dashboard Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=dashboard"),
                CreateTestRunWithAppUrl("Settings Page Type", "https://contoso.crm.dynamics.com/main.aspx?pagetype=settings")
            };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(2, result.Count); // Should have 2 groups: dashboard and settings 
            Assert.True(result.ContainsKey("dashboard"));
            Assert.True(result.ContainsKey("settings"));
            Assert.Equal(2, result["dashboard"].Count); // Two dashboard tests
            Assert.Single(result["settings"]); // One settings test
        }        [Fact]
        public void GroupTestsByRun_MixedTestTypes_GroupsCorrectly()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            var testRuns = new List<TestRun>
            {
                // Entity list
                CreateTestRunWithAppUrl("Account List Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account"),
                // Entity record
                CreateTestRunWithAppUrl("Account Record Test", "https://contoso.crm.dynamics.com/main.aspx?pagetype=entity&etn=account&id=123"),
                // Custom page with name
                CreateTestRunWithAppUrl("Dashboard Custom Page", "https://contoso.crm.dynamics.com/main.aspx?pagetype=custom&name=test"),
                // Custom page type
                CreateTestRunWithAppUrl("Dashboard Page Type", "https://contoso.crm.dynamics.com/main.aspx?pagetype=dashboard")
            };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(3, result.Count); // Should have 3 groups: account, test, and dashboard
            Assert.True(result.ContainsKey("account"));
            Assert.True(result.ContainsKey("test"));
            Assert.True(result.ContainsKey("dashboard"));
            Assert.Equal(2, result["account"].Count); // Two account tests (list and record)
            Assert.Single(result["test"]); // One dashboard custom page test
            Assert.Single(result["dashboard"]); // One dashboard page type test
        }

        [Fact]
        public void GroupTestsByRun_NoAppUrl_GroupsByTestRunName()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
              // Create test runs without app URLs
            var testRun1 = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Account Tests", 
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test1",
                            Outcome = TestReporter.PassedResultOutcome,
                            Output = new TestOutput { StdOut = "Some output without AppURL" }
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput { StdOut = "No AppURL here" }
                }
            };

            var testRun2 = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Contact Tests",
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test2",
                            Outcome = TestReporter.PassedResultOutcome,
                            Output = new TestOutput { StdOut = "Some output without AppURL" }
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput { StdOut = "Different output without AppURL" }
                }
            };

            var testRuns = new List<TestRun> { testRun1, testRun2 };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(2, result.Count); // Should have 2 groups based on test run names
            Assert.True(result.ContainsKey("Account Tests"));
            Assert.True(result.ContainsKey("Contact Tests"));
            Assert.Single(result["Account Tests"]);
            Assert.Single(result["Contact Tests"]);
        }

        [Fact]
        public void GroupTestsByRun_WithNullResultSummary_GroupsByTestRunName()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            
            // Create test run with null ResultSummary
            var testRun1 = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Run With Null ResultSummary", 
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test1",
                            Outcome = TestReporter.PassedResultOutcome
                        }
                    }
                },
                ResultSummary = null
            };

            // Create test run with null Output in ResultSummary
            var testRun2 = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Run With Null Output", 
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test2",
                            Outcome = TestReporter.PassedResultOutcome
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = null
                }
            };

            var testRuns = new List<TestRun> { testRun1, testRun2 };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey("Test Run With Null ResultSummary"));
            Assert.True(result.ContainsKey("Test Run With Null Output"));
            Assert.Single(result["Test Run With Null ResultSummary"]);
            Assert.Single(result["Test Run With Null Output"]);
        }

        [Fact]
        public void GroupTestsByRun_JsonAppUrlInSummary_ParsesCorrectly()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            
            // Create test run with JSON AppURL in ResultSummary.Output.StdOut
            var testRun = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "JSON Test Run", 
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test1",
                            Outcome = TestReporter.PassedResultOutcome,
                            Output = new TestOutput { StdOut = "Some regular output" }
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput 
                    { 
                        StdOut = @"{
                            ""AppURL"": ""https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=contact"",
                            ""VideoPath"": ""C:\\path\\to\\video.mp4"",
                            ""OtherProperty"": ""value""
                        }"
                    }
                }
            };

            var testRuns = new List<TestRun> { testRun };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Single(result);
            Assert.True(result.ContainsKey("contact"));
            Assert.Single(result["contact"]);
        }

        [Fact]
        public void GroupTestsByRun_NullOutput_HandlesGracefully()
        {
            // Arrange
            var testRunSummary = new TestRunSummary(MockFileSystem.Object);
            
            // Create test run with null Output in UnitTestResult
            var testRun = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test With Null Output", 
                Results = new TestResults
                {
                    UnitTestResults = new List<UnitTestResult>
                    {
                        new UnitTestResult
                        {
                            TestId = Guid.NewGuid().ToString(),
                            TestName = "Test1",
                            Outcome = TestReporter.PassedResultOutcome,
                            Output = null
                        }
                    }
                },
                ResultSummary = new TestResultSummary
                {
                    Output = new TestOutput 
                    { 
                        StdOut = @"{ ""AppURL"": ""https://contoso.crm.dynamics.com/main.aspx?pagetype=entitylist&etn=account"" }"
                    }
                }
            };

            var testRuns = new List<TestRun> { testRun };

            // Act
            var result = testRunSummary.GroupTestsByRun(testRuns);

            // Assert
            Assert.Single(result);
            Assert.True(result.ContainsKey("account"));
            Assert.Single(result["account"]);
        }
    }
}
