// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

        public ScreenshotFunction(ITestInfraFunctions testInfraFunctions, ISingleTestInstanceState singleTestInstanceState, IFileSystem fileSystem) 
            : base("Screenshot", FormulaType.Blank, FormulaType.String)
        {
            _testInfraFunctions = testInfraFunctions;
            _singleTestInstanceState = singleTestInstanceState;
            _fileSystem = fileSystem;
        }

        public BlankValue Execute(StringValue file)
        {
            var testResultDirectory = _singleTestInstanceState.GetTestResultsDirectory();
            if (!_fileSystem.IsValidFilePath(testResultDirectory))
            {
                throw new InvalidOperationException("Test result directory needs to be set");
            }

            var fileName = file.Value;

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException(nameof(fileName));
            }

            if (Path.IsPathRooted(fileName))
            {
                throw new ArgumentException("Only support relative file paths");
            }

            if (!fileName.EndsWith(".jpg") && !fileName.EndsWith(".jpeg") && !fileName.EndsWith("png"))
            {
                throw new ArgumentException("Only support jpeg and png files");
            }

            var filePath = Path.Combine(testResultDirectory, fileName);
            _testInfraFunctions.ScreenshotAsync(filePath).Wait();

            return FormulaValue.NewBlank();
        }
    }
}
