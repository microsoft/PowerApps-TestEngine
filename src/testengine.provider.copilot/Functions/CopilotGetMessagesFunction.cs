using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers.Functions
{
    public class CopilotGetMessagesFunction : ReflectionFunction
    {
        private readonly CopilotProvider _provider;
        private readonly Extensions.Logging.ILogger? _logger;

        public CopilotGetMessagesFunction(CopilotProvider provider, Extensions.Logging.ILogger? logger)
            : base(DPath.Root.Append(new DName("Preview")), "CopilotGetMessages", TableType.Empty())
        {
            _provider = provider;
            _logger = logger;
        }

        public TableValue Execute()
        {
            try
            {
                var messages = _provider.GetLatestMessages();
                var records = messages.Select(m => FormulaValue.NewRecordFromFields(
                    new NamedValue("Type", FormulaValue.New(m.Type ?? "")),
                    new NamedValue("Text", FormulaValue.New(m.Text ?? "")),
                    new NamedValue("Id", FormulaValue.New(m.Id ?? ""))
                ));

                return FormulaValue.NewTable(RecordType.Empty().Add("Type", StringType.String).Add("Text", StringType.String).Add("Id", StringType.String), records.ToArray());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in CopilotGetMessages function");
                return FormulaValue.NewTable(RecordType.Empty());
            }
        }
    }
}
