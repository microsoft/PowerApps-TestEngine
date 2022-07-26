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
using System.Reflection.Metadata.Ecma335;
using Microsoft.PowerApps.TestEngine.TestStudioConverter;

var switchMappings = new Dictionary<string, string>()
{
    { "-i", "TestPlanFile" },
    { "-e", "EnvironmentId" },
    { "-t", "TenantId" },
    { "-o", "OutputDirectory" }
};

if (args.Length > 1)
{
    if (args[0].Equals("convert"))
    {
        string InputDir = args[1];

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { builder.ClearProviders(); builder.AddConsole(); });
        ILogger<CreateYAMLTestPlan> logger = loggerFactory.CreateLogger<CreateYAMLTestPlan>();
        CreateYAMLTestPlan converter = new CreateYAMLTestPlan(logger, InputDir);
        converter.exportYAML();
        return;
    }
}

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
    var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder
        .ClearProviders()
        .AddProvider(new TestLoggerProvider(new FileSystem()));
    })
    .AddScoped<ITestInfraFunctions, PlaywrightTestInfraFunctions>()
    .AddSingleton<ITestConfigParser, YamlTestConfigParser>()
    .AddScoped<IPowerFxEngine, PowerFxEngine>()
    .AddScoped<IUserManager, UserManager>()
    .AddSingleton<ITestState, TestState>()
    .AddScoped<IUrlMapper, PowerAppsUrlMapper>()
    .AddScoped<IPowerAppFunctions, PowerAppFunctions>()
    .AddSingleton<ITestReporter, TestReporter>()
    .AddScoped<ISingleTestInstanceState, SingleTestInstanceState>()
    .AddScoped<ISingleTestRunner, SingleTestRunner>()
    .AddSingleton<IFileSystem, FileSystem>()
    .AddSingleton<IEnvironmentVariable, EnvironmentVariable>()
    .AddSingleton<TestEngine>()
    .BuildServiceProvider();

    // Issue: Config tells us what logging settings we're supposed to set
    // However, logging settings are being set above, and we don't have the config yet
    // Yet, We can only get the config by first setting the above. But need the config to accurately set the above
    // Seems like chicken/egg problem


    TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();

    var testResult = await testEngine.RunTestAsync(inputOptions.TestPlanFile, inputOptions.EnvironmentId, inputOptions.TenantId, inputOptions.OutputDirectory);

    Console.Out.WriteLine($"Test results can be found here: {testResult}");
}
