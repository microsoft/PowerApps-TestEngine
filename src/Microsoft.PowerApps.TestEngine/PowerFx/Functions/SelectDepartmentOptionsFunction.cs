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
    /// Power Fx function to select a department option in the Department dropdown.
    /// </summary>
    public class SelectDepartmentOptionsFunction : ReflectionFunction
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly ILogger _logger;

        public SelectDepartmentOptionsFunction(ITestWebProvider testWebProvider, ILogger logger)
            : base("SelectDepartmentOptions", FormulaType.Boolean, FormulaType.String)
        {
            _testWebProvider = testWebProvider;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue department)
        {
            return ExecuteAsync(department).Result;
        }

        /// <summary>
        /// Asynchronously selects a department option in the Department dropdown.
        /// </summary>
        public async Task<BooleanValue> ExecuteAsync(StringValue department)
        {
            _logger.LogInformation($"Executing SelectDepartmentOptionsFunction for department '{department.Value}'.");
            await _testWebProvider.TestInfraFunctions.SelectDepartmentOptionsAsync(department.Value);
            _logger.LogInformation("SelectDepartmentOptionsFunction execution completed.");
            return FormulaValue.New(true);
        }       
    }
}
