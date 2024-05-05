// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Modules;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerApps.TestEngine.Users;
using PowerAppsTestEngine;

var switchMappings = new Dictionary<string, string>()
{
    { "-i", "TestPlanFile" },
    { "-e", "EnvironmentId" },
    { "-t", "TenantId" },
    { "-o", "OutputDirectory" },
    { "-l", "LogLevel" },
    { "-q", "QueryParams" },
    { "-d", "Domain" },
    { "-m", "Modules" },
    { "-u", "UserAuth" }
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
    Console.WriteLine("[Critical Error]: Input options are null");
    return;
}
else
{

    // If an empty field is put in via commandline, it won't register as empty
    // It will cannabalize the next flag, and then ruin the next flag's operation
    // Therefore, we have to abort the program in this instance

    if (!string.IsNullOrEmpty(inputOptions.TestPlanFile))
    {
        if (inputOptions.TestPlanFile.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: TestPlanFile field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.EnvironmentId))
    {
        if (inputOptions.EnvironmentId.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: EnvironmentId field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.TenantId))
    {
        if (inputOptions.TenantId.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: TenantId field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.OutputDirectory))
    {
        if (inputOptions.OutputDirectory.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: OutputDirectory field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.LogLevel))
    {
        if (inputOptions.LogLevel.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: LogLevel field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.Domain))
    {
        if (inputOptions.Domain.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: Domain field is blank.");
            return;
        }
    }

    if (!string.IsNullOrEmpty(inputOptions.QueryParams))
    {
        if (inputOptions.QueryParams.Substring(0, 1) == "-")
        {
            Console.WriteLine("[Critical Error]: QueryParams field is blank.");
            return;
        }
    }

    var logLevel = LogLevel.Information; // Default log level
    if (string.IsNullOrEmpty(inputOptions.LogLevel))
    {
        Console.WriteLine($"Unable to parse log level: {inputOptions.LogLevel}, using default");
        Enum.TryParse(inputOptions.LogLevel, true, out logLevel);
    }

    var userAuth ="browser"; // Default to brower authentication
    if (!string.IsNullOrEmpty(inputOptions.UserAuth))
    {
        userAuth = inputOptions.UserAuth;
    }

    var provider = "canvas";

    try
    {
        using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .ClearProviders()
            .AddFilter(l => l >= logLevel)
            .AddProvider(new TestLoggerProvider(new FileSystem())));

        var logger = loggerFactory.CreateLogger<Program>();

        var serviceProvider = new ServiceCollection()
        .AddSingleton<ILoggerFactory>(loggerFactory)
        .AddSingleton<ITestEngineEvents, TestEngineEventHandler>()
        .AddSingleton<ITestConfigParser, YamlTestConfigParser>()
        .AddScoped<IPowerFxEngine, PowerFxEngine>()
        .AddScoped<IUserManager>(sp =>
        {
            var testState = sp.GetRequiredService<ITestState>();
            var userManagers = testState.GetTestEngineUserManager();
            if (userManagers.Count == 0)
            {
                testState.LoadExtensionModules(logger);
                userManagers = testState.GetTestEngineUserManager();
            }
            return userManagers.Where(x => x.Name.Equals(userAuth)).First();
        })
        .AddTransient<ITestWebProvider>(sp => {
            var testState = sp.GetRequiredService<ITestState>();
            var testWebProviders = testState.GetTestEngineWebProviders();
            if (testWebProviders.Count == 0)
            {
                testState.LoadExtensionModules(logger);
                testWebProviders = testState.GetTestEngineWebProviders();
            }
            return testWebProviders.Where(x => x.Name.Equals(provider)).First();
        })
        .AddSingleton<ITestState, TestState>()
        .AddSingleton<ITestReporter, TestReporter>()
        .AddScoped<ISingleTestInstanceState, SingleTestInstanceState>()
        .AddScoped<ISingleTestRunner, SingleTestRunner>()
        .AddScoped<ILogger>((sp) => sp.GetRequiredService<ISingleTestInstanceState>().GetLogger())
        .AddSingleton<IFileSystem, FileSystem>()
        .AddScoped<ITestInfraFunctions, PlaywrightTestInfraFunctions>()
        .AddSingleton<IEnvironmentVariable, EnvironmentVariable>()
        .AddSingleton<TestEngine>()
        
        .BuildServiceProvider();

        TestEngine testEngine = serviceProvider.GetRequiredService<TestEngine>();

        // Default value for optional arguments is set before the class library is invoked.
        // The class library expects actual types in its input arguments, so optional arguments
        // to the Test Engine entry point function RunTestAsync must be checked for null values and their
        // corresponding default values set beforehand.
        var testPlanFile = new FileInfo(inputOptions.TestPlanFile);
        var tenantId = Guid.Parse(inputOptions.TenantId);
        var environmentId = inputOptions.EnvironmentId;
        var domain = "apps.powerapps.com";
        var queryParams = "";

        DirectoryInfo outputDirectory;
        const string DefaultOutputDirectory = "TestOutput";
        if (!string.IsNullOrEmpty(inputOptions.OutputDirectory))
        {
            outputDirectory = new DirectoryInfo(inputOptions.OutputDirectory);
        }
        else
        {
            outputDirectory = new DirectoryInfo(DefaultOutputDirectory);
        }

        if (!string.IsNullOrEmpty(inputOptions.QueryParams))
        {
            queryParams = inputOptions.QueryParams;
        }

        if (!string.IsNullOrEmpty(inputOptions.Domain))
        {
            domain = inputOptions.Domain;
        }

        string modulePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        List<ITestEngineModule> modules = new List<ITestEngineModule>();
        if (!string.IsNullOrEmpty(inputOptions.Modules) && Directory.Exists(inputOptions.Modules))
        {
            modulePath = inputOptions.Modules;
        }

        ITestState state = serviceProvider.GetService<ITestState>();
        state.SetModulePath(modulePath);

        
        
        //setting defaults for optional parameters outside RunTestAsync
        var testResult = await testEngine.RunTestAsync(testPlanFile, environmentId, tenantId, outputDirectory, domain, queryParams);
        if (testResult != "InvalidOutputDirectory")
        {
            Console.WriteLine($"Test results can be found here: {testResult}");
        }

    }
    catch (Exception ex)
    {
        Console.WriteLine("[Critical Error]: " + ex.Message);
        Console.WriteLine(ex);
    }
}


