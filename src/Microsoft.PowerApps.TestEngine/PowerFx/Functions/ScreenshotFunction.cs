// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will capture a screenshot of the app at the current point in time. 
    /// The screenshot file will be saved to the test output folder and with the name provided.
    /// </summary>
    public class ScreenshotFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ISingleTestInstanceState _singleTestInstanceState;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public ScreenshotFunction(ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem, ILogger logger)
            : base("Screenshot", FormulaType.Blank, FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public BlankValue Execute(StringValue file)
        {
            _logger.LogInformation("------------------------------\n\n" +
                "Executing Screenshot function.");

            var testResultDirectory = _singleTestInstanceState.GetTestResultsDirectory();
            if (!_fileSystem.IsValidFilePath(testResultDirectory))
            {
                _logger.LogError("Test result directory needs to be set.");
                throw new InvalidOperationException();
            }

            var fileName = file.Value;

            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogTrace("File Name: " + nameof(fileName));
                _logger.LogError("File must exist and cannot be empty.");
                throw new ArgumentException();
            }

            if (Path.IsPathRooted(fileName))
            {
                _logger.LogError("Only support relative file paths");
                throw new ArgumentException();
            }

            if (!fileName.EndsWith(".jpg") && !fileName.EndsWith(".jpeg") && !fileName.EndsWith("png"))
            {
                _logger.LogDebug("File extension: " + Path.GetExtension(fileName));
                _logger.LogTrace("File name: " + fileName);
                _logger.LogError("Only support jpeg and png files");
                throw new ArgumentException();
            }

            var filePath = Path.Combine(testResultDirectory, fileName);
            _testInfraFunctions.ScreenshotAsync(filePath).Wait();

            _logger.LogInformation("Successfully finished executing Screenshot function.");

            return FormulaValue.NewBlank();
        }
    }
}
