using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
     internal class CopilotSendMessageFunction : ReflectionFunction
    {
        private readonly CopilotProvider _provider;
        private readonly Extensions.Logging.ILogger? _logger;

        public CopilotSendMessageFunction(CopilotProvider provider, Extensions.Logging.ILogger? logger) 
            : base(DPath.Root.Append(new DName("Preview")), "CopilotSendMessage", FormulaType.Boolean, StringType.String)
        {
            _provider = provider;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue message)
        {
            try
            {
                var result = _provider.SendMessageAsync(message.Value).GetAwaiter().GetResult();
                return FormulaValue.New(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in CopilotSendMessage function");
                return FormulaValue.New(false);
            }
        }
    }
}
