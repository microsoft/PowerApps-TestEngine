using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;



namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{ 
  /// <summary>
    /// Navigates to the selected entity record in the grid, or to the new record page if none selected.
    /// </summary>
    public class NavigateToRecordFunction : ReflectionFunction
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly Func<Task> _updateModelFunction;
        private readonly ILogger _logger;

       
        public NavigateToRecordFunction(ITestWebProvider testWebProvider, Func<Task> updateModelFunction, ILogger logger)
            : base("NavigateToRecord", FormulaType.Boolean, FormulaType.String, FormulaType.String, FormulaType.Number)
        {
            _testWebProvider = testWebProvider;
            _updateModelFunction = updateModelFunction;
            _logger = logger;
        }

        public BooleanValue Execute(
            StringValue entityName,
            StringValue entityPage,
            NumberValue target)
        {
            return ExecuteAsync(entityName, entityPage, target).Result;
        }

        public async Task<BooleanValue> ExecuteAsync(
            StringValue entityName,
            StringValue entityPage,
            NumberValue target)
        {
            _logger.LogInformation("Executing NavigateToRecordFunction: extracting selected entityId from grid.");

            // Extract the selected entityId from the grid
            var jsExtractId = @"
                        (function() {
                            const selectedRows = document.querySelectorAll(""div[role='row'][aria-label*='deselect']"");
                            for (let row of selectedRows) {
                                const link = row.querySelector(""a[aria-label][href*='etn=" + entityName.Value + @"']"");
                                if (link) {
                                    const url = new URL(link.href, window.location.origin);
                                    const entityId = url.searchParams.get('id');
                                    if (entityId) {
                                        return entityId;
                                    }
                                }
                            }
                            return '';
                        })();
                    ";

            var page = _testWebProvider.TestInfraFunctions.GetContext().Pages.First();
            var entityId = await page.EvaluateAsync<string>(jsExtractId);

            var pageInput = new JObject
            {
                ["pageType"] = entityPage.Value,
                ["entityName"] = entityName.Value
            };

            if (!string.IsNullOrEmpty(entityId))
            {
                pageInput["entityId"] = entityId;
                _logger.LogInformation($"Navigating to existing record with entityId: {entityId}");
            }
            else
            {
                _logger.LogInformation("No selected entity found. Navigating to new record page.");
            }

            var navigationOptions = new JObject
            {
                ["target"] = (int)target.Value
            };

            var jsNavigate = $@"
                        Xrm.Navigation.navigateTo({pageInput}, {navigationOptions})
                            .then(function() {{ return true; }}, function(error) {{ return false; }});
                    ";

            var navResult = await page.EvaluateAsync<bool>(jsNavigate);
            _logger.LogInformation($"Navigation result: {navResult}");

            // Ensure Power Fx model is updated after navigation
            await _updateModelFunction();

            return FormulaValue.New(navResult);
        }
    }
}
