// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// This will capture a screenshot of the app at the current point in time. 
    /// The screenshot file will be saved to the test output folder and with the name provided.
    /// </summary>
    public class ScreenshotFunction : ReflectionFunction
    {
        private ITestInfraFunctions TestInfraFunctions { get; set; }
        private string OutputDirectory { get; set; }

        public ScreenshotFunction(ITestInfraFunctions testInfraFunctions, string outputDirectory) 
            : base("Screenshot", FormulaType.Boolean, FormulaType.String)
        {
            TestInfraFunctions = testInfraFunctions;
            OutputDirectory = outputDirectory;
        }

        public BooleanValue Execute(StringValue file)
        {
            TestInfraFunctions.ScreenshotAsync($"{OutputDirectory}/{file.Value}").Wait();

            return FormulaValue.New(true);
        }
    }
}
