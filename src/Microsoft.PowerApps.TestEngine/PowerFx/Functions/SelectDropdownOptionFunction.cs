using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// Power Fx function to select an option in a dropdown.
    /// </summary>
    public class SelectDropdownOptionFunction : ReflectionFunction
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly ILogger _logger;

        public SelectDropdownOptionFunction(ITestWebProvider testWebProvider, ILogger logger)
            : base("SelectDropdownOption", FormulaType.Boolean, FormulaType.String)
        {
            _testWebProvider = testWebProvider;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue department)
        {
            return ExecuteAsync(department).Result;
        }

        /// <summary>
        /// Asynchronously selects a dropdown option in the dropdown.
        /// </summary>
        public async Task<BooleanValue> ExecuteAsync(StringValue dropdownOption)
        {
            if (dropdownOption == null)
                throw new ArgumentNullException(nameof(dropdownOption));

            _logger.LogInformation($"Executing SelectDropdownOptionFunction for dropdown '{dropdownOption.Value}'.");
            await _testWebProvider.TestInfraFunctions.SelectDropdownOptionAsync(dropdownOption.Value);
            _logger.LogInformation("SelectDropdownOptionFunction execution completed.");
            return FormulaValue.New(true);
        }
    }
}
