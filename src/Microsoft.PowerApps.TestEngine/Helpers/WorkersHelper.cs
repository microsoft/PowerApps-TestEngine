// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerApps.TestEngine.Config;

namespace Microsoft.PowerApps.TestEngine.Helpers
{
    /// <summary>
    /// A helper class to test the total times of test run for workers.
    /// </summary>
    public class WorkersHelper
    {
        private readonly ITestState _state;
        private readonly IServiceProvider _serviceProvider;
        public int TotalTestRun(string testRunId, string testRunDirectory, List<TestDefinition> testDefinitions, TestSettings testSettings)
        {
            int workers = testSettings.Workers;
            if (workers < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            var browserConfigurations = testSettings.BrowserConfigurations;

            Queue<Task> allTestRuns = new Queue<Task>();
            int count = 0;

            // Manage number of workers
            foreach (var eachTestDefinition in testDefinitions)
            {
                foreach (var eachBrowserConfig in browserConfigurations)
                {
                    allTestRuns.Enqueue(RunOneTestAsync(testRunId, testRunDirectory, eachTestDefinition, eachBrowserConfig));
                    if (allTestRuns.Count >= workers)
                    {
                        var maxTestRuns = new List<Task>();
                        while (allTestRuns.Count > 0)
                        {
                            maxTestRuns.Add(allTestRuns.Dequeue());
                        }
                        //await Task.WhenAll(maxTestRuns.ToArray());
                        count++;
                    }
                }
            }
            var restTestRuns = new List<Task>();
            while (allTestRuns.Count > 0)
            {
                restTestRuns.Add(allTestRuns.Dequeue());
            }
            //await Task.WhenAll(restTestRuns.ToArray());
            if (restTestRuns.Count > 0) count++;
            return count;
        }


        private async Task RunOneTestAsync(string testRunId, string testRunDirectory, TestDefinition testDefinition, BrowserConfiguration browserConfig)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var singleTestRunner = scope.ServiceProvider.GetRequiredService<ISingleTestRunner>();
                await singleTestRunner.RunTestAsync(testRunId, testRunDirectory, testDefinition, browserConfig);
            }
        }

    }
}