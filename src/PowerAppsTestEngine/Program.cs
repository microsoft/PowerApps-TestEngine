// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using PowerAppsTestEngine;

using var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
        {
            loggingBuilder
            .ClearProviders()
            .AddConsole() // TODO: figure out why I can't have both console logging and the test logger at the same time.
            .AddProvider(new TestLoggerProvider(new FileSystem()));
        })
    .BuildServiceProvider();

var switchMappings = new Dictionary<string, string>()
{
    { "-i", "TestPlanFile" },
    { "-e", "EnvironmentId" },
    { "-t", "TenantId" },
    { "-o", "OutputDirectory" }
};

var inputOptions = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config.json", true)
    .AddJsonFile("config.dev.json", true)
    .AddCommandLine(args, switchMappings)
    .Build()
    .Get<InputOptions>();

if (inputOptions == null)
{
    Console.Out.WriteLine("Input options are null");
    return;
} else
{
    TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();

    var testResult = await testEngine.RunTestAsync(inputOptions.TestPlanFile, inputOptions.EnvironmentId, inputOptions.TenantId, inputOptions.OutputDirectory);

    Console.Out.WriteLine($"Test results can be found here: {testResult}");
}