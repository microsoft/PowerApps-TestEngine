using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
    internal class CopilotWaitForResponseFunction : ReflectionFunction
    {
        private readonly CopilotProvider _provider;
        private readonly ITestInfraFunctions? _testInfraFunctions;
        private readonly ITestState? _testState;
        private readonly Extensions.Logging.ILogger? _logger;

        public CopilotWaitForResponseFunction(CopilotProvider provider, ITestInfraFunctions? testInfraFunctions, ITestState? testState, Extensions.Logging.ILogger? logger) 
            : base(DPath.Root.Append(new DName("Preview")), "CopilotWaitForResponse", FormulaType.Boolean, NumberType.Number)
        {
            _provider = provider;
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        public BooleanValue Execute(NumberValue timeoutSeconds)
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
                var initialCount = _provider.Messages.Count;
                var endTime = DateTime.UtcNow.Add(timeout);

                while (DateTime.UtcNow < endTime)
                {
                    if (_provider.Messages.Count > initialCount)
                    {
                        return FormulaValue.New(true);
                    }
                    
                    Thread.Sleep(100); // Small delay
                }
                
                return FormulaValue.New(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in CopilotWaitForResponse function");
                return FormulaValue.New(false);
            }
        }
    }
}
