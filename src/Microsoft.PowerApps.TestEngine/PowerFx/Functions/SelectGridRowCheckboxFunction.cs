using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// Selects or deselects a checkbox in a grid row based on the row index.
    /// </summary>
    public class SelectGridRowCheckboxFunction : ReflectionFunction
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly ILogger _logger;

        public SelectGridRowCheckboxFunction(ITestWebProvider testWebProvider, ILogger logger)
            : base("SelectGridRowCheckbox", FormulaType.Boolean, FormulaType.Number)
        {
            _testWebProvider = testWebProvider;
            _logger = logger;
        }

        public BooleanValue Execute(NumberValue rowIndex)
        {
            return ExecuteAsync(rowIndex).Result;
        }

        public async Task<BooleanValue> ExecuteAsync(NumberValue rowIndex)
        {
            _logger.LogInformation($"Executing SelectGridRowCheckboxFunction for row index {rowIndex.Value}.");

            var js = $@"
                (function() {{
                    var checkboxes = document.querySelectorAll(""input[type='checkbox'][aria-label='select or deselect the row']"");
                    var idx = {rowIndex.Value};
                    if (checkboxes.length > idx) {{
                        checkboxes[idx].click();
                        console.log('Checkbox in row ' + (idx + 1) + ' clicked.');
                    }} else {{
                        console.log('Row index ' + idx + ' is out of bounds. Only ' + checkboxes.length + ' checkbox(es) found.');
                    }}
                }})();
            ";

            var page = _testWebProvider.TestInfraFunctions.GetContext().Pages.First();
            await page.EvaluateAsync(js);

            _logger.LogInformation("SelectGridRowCheckboxFunction execution completed.");
            return FormulaValue.New(true);
        }
    }
}
