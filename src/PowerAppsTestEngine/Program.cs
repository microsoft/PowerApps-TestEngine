// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.PowerApps.TestEngine;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerApps;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.TestStudioConverter;
using Microsoft.PowerApps.TestEngine.Users;
using PowerAppsTestEngine;



var switchMappings = new Dictionary<string, string>()
{
    { "-i", "TestPlanFile" },
    { "-e", "EnvironmentId" },
    { "-t", "TenantId" },
    { "-o", "OutputDirectory" },
    { "-l", "LogLevel" },
    { "-q", "QueryParams" }
};

if (args.Length > 1)
{
    if (args[0].Equals("convert"))
    {
        string InputDir = args[1];

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => { builder.ClearProviders(); builder.AddConsole(); });
        ILogger<CreateYamlTestPlan> logger = loggerFactory.CreateLogger<CreateYamlTestPlan>();
        CreateYamlTestPlan converter = new CreateYamlTestPlan(logger, InputDir);
        converter.ExportYaml();
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
}
else
{
    var logLevel = LogLevel.Information; // Default log level
    if (!string.IsNullOrEmpty(inputOptions.LogLevel) && !Enum.TryParse(inputOptions.LogLevel, true, out logLevel))
    {
        Console.Out.WriteLine($"Unable to parse log level: {inputOptions.LogLevel}, using default: Information");
    }

    var serviceProvider = new ServiceCollection()
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder
        .ClearProviders()
        .AddFilter(l => l >= logLevel)
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


    TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();

    var queryParams = "";

    if (!string.IsNullOrEmpty(inputOptions.QueryParams))
    {
        queryParams = inputOptions.QueryParams;
    }

    var testResult = await testEngine.RunTestAsync(inputOptions.TestPlanFile, inputOptions.EnvironmentId, inputOptions.TenantId, inputOptions.OutputDirectory, "", queryParams);

    Console.Out.WriteLine($"Test results can be found here: {testResult}");
}
