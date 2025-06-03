using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// Sets the Date of Birth and Time of Birth fields using aria-label selectors.
    /// </summary>
    public class SetDOBFieldsFunction : ReflectionFunction
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly ILogger _logger;

        public SetDOBFieldsFunction(ITestWebProvider testWebProvider, ILogger logger)
            : base("SetDOBFields", FormulaType.Boolean, FormulaType.String)
        {
            _testWebProvider = testWebProvider;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue dateValue)
        {
            return ExecuteAsync(dateValue).Result;
        }

        public async Task<BooleanValue> ExecuteAsync(StringValue dateValue)
        {
            _logger.LogInformation("Executing SetDOBFieldsFunction with provided values.");
            var js = $@"
            (async function() {{
                const expectedValue = '{DateTime.Parse(dateValue.Value).ToString("M/d/yyyy")}';
                const dateElement = document.querySelector(""input[aria-label='Date of DOB']"");

                if (!dateElement) {{
                    console.warn('❌ Date input not found.');
                    return;
                }}

                const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                dateElement.focus();
                nativeInputValueSetter.call(dateElement, expectedValue);
                ['input', 'change', 'blur'].forEach(eventType => {{
                    dateElement.dispatchEvent(new Event(eventType, {{ bubbles: true }}));
                }});

                // Wait for the value to be accepted
                await new Promise(resolve => setTimeout(resolve, 1000));

                if (dateElement.value === expectedValue) {{
                    console.log('✅ Date of DOB successfully set to', dateElement.value);
                }} else {{
                    console.warn('❌ Date of DOB was rejected or reverted. Current value:', dateElement.value);
                }}
            }})();";


            var page = _testWebProvider.TestInfraFunctions.GetContext().Pages.First();
            await page.EvaluateAsync(js);

            _logger.LogInformation("SetDOBFieldsFunction execution completed.");
            return FormulaValue.New(true);
        }
    }
}


//var dateElement = document.querySelector(""[aria - label = 'Date of DOB']"");
//dateElement.click();
//dateElement.value = '{dateValue.Value}';
//dateElement.dispatchEvent(new Event('input', { { bubbles: true } }));
//console.log('Date of DOB set.');
