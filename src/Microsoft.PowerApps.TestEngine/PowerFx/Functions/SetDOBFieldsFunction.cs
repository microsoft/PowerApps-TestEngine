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
            : base("SetDOBFields", FormulaType.Boolean, FormulaType.String, FormulaType.String)
        {
            _testWebProvider = testWebProvider;
            _logger = logger;
        }

        public BooleanValue Execute(StringValue dateValue, StringValue timeValue)
        {
            return ExecuteAsync(dateValue, timeValue).Result;
        }

        public async Task<BooleanValue> ExecuteAsync(StringValue dateValue, StringValue timeValue)
        {
            _logger.LogInformation($"Executing SetDOBFieldsFunction with provided values: date={dateValue.Value}, time={timeValue.Value}");

            // Default time to 08:00 AM if not provided
            var time = string.IsNullOrWhiteSpace(timeValue.Value) ? "08:00 AM" : timeValue.Value;

            var js = $@"
                (async function() {{
                    function waitForElement(selector, timeout = 5000) {{
                        return new Promise((resolve, reject) => {{
                            const interval = 100;
                            let elapsed = 0;
                            const check = () => {{
                                const el = document.querySelector(selector);
                                if (el) return resolve(el);
                                elapsed += interval;
                                if (elapsed >= timeout) return reject('Timeout: ' + selector);
                                setTimeout(check, interval);
                            }};
                            check();
                        }});
                    }}

                    const dateStr = '{dateValue.Value}';
                    const timeStr = '{time.Replace("'", "\\'")}';
                    console.log('SetDOBFieldsFunction JS: dateStr=', dateStr, 'timeStr=', timeStr);
                    const [month, day, year] = dateStr.split('/').map(part => parseInt(part, 10));
                    const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
                                        'July', 'August', 'September', 'October', 'November', 'December'];
                    const monthName = monthNames[month - 1];

                    try {{
                        const dateInput = await waitForElement(""[aria-label='Date of DOB']"");
                        const calendarIcon = dateInput.parentElement.querySelector('svg');
                        if (!calendarIcon) {{
                            console.log('Calendar icon not found.');
                            return;
                        }}

                        // Step 1: Open calendar
                        calendarIcon.dispatchEvent(new MouseEvent('click', {{ bubbles: true }}));
                        console.log('Calendar opened.');

                        // Step 2: Switch to year picker
                        const yearSwitchBtn = await waitForElement(""button[aria-label*='select to switch to year picker']"");
                        yearSwitchBtn.click();
                        console.log('Switched to year picker.');

                        // Step 3: Navigate to correct year
                        async function selectYear() {{
                            let yearBtn = Array.from(document.querySelectorAll('button'))
                                .find(btn => btn.textContent.trim() === String(year));
                            let attempts = 0;
                            while (!yearBtn && attempts < 15) {{
                                const prevYearBtn = document.querySelector(""button[title*='Navigate to previous year']"");
                                if (prevYearBtn) {{
                                    prevYearBtn.click();
                                    await new Promise(r => setTimeout(r, 200));
                                }}
                                yearBtn = Array.from(document.querySelectorAll('button'))
                                    .find(btn => btn.textContent.trim() === String(year));
                                attempts++;
                            }}
                            if (yearBtn) {{
                                yearBtn.click();
                                console.log(`Year ${{year}} selected.`);
                            }} else {{
                                console.warn('Year button not found.');
                                return false;
                            }}
                            return true;
                        }}
                        if (!await selectYear()) return;

                        // Step 4: Select month
                        const monthBtn = Array.from(document.querySelectorAll(""button[role='gridcell']""))
                            .find(btn => btn.textContent.trim().toLowerCase().startsWith(monthName.slice(0, 3).toLowerCase()));
                        if (monthBtn) {{
                            monthBtn.click();
                            console.log(`Month ${{monthName}} selected.`);
                        }} else {{
                            console.warn(`Month ${{monthName}} not found.`);
                            return;
                        }}

                        // Step 5: Select day
                        await new Promise(r => setTimeout(r, 400));
                        const dayBtn = Array.from(document.querySelectorAll(""td button[aria-label]""))
                            .find(btn => btn.getAttribute('aria-label')?.includes(`${{day}}, ${{monthName}}, ${{year}}`));
                        if (dayBtn) {{
                            dayBtn.click();
                            console.log(`Day ${{day}} selected.`);
                        }} else {{
                            console.warn(`Day ${{day}} not found.`);
                            return;
                        }}

                        // Step 6: Set time
                        await new Promise(r => setTimeout(r, 600));
                        const timeInput = await waitForElement(""[aria-label='Time of DOB']"");
                        timeInput.focus();
                        timeInput.value = timeStr;
                        timeInput.setAttribute('value', timeStr);
                        [
                            'focus', 'keydown', 'keypress', 'input', 'keyup', 'change', 'blur', 'focusout', 'click', 'paste',
                            'compositionstart', 'compositionupdate', 'compositionend'
                        ].forEach(eventType => {{
                            timeInput.dispatchEvent(new Event(eventType, {{ bubbles: true }}));
                        }});
                        setTimeout(() => {{
                            if (timeInput.value !== timeStr) {{
                                timeInput.value = timeStr;
                                timeInput.setAttribute('value', timeStr);
                                [
                                    'focus', 'keydown', 'keypress', 'input', 'keyup', 'change', 'blur', 'focusout', 'click', 'paste',
                                    'compositionstart', 'compositionupdate', 'compositionend'
                                ].forEach(eventType => {{
                                    timeInput.dispatchEvent(new Event(eventType, {{ bubbles: true }}));
                                }});
                                console.log('Retried setting time to:', timeStr);
                            }}
                        }}, 300);
                        console.log(`Time set to ${{timeStr}}.`);
                    }} catch (e) {{
                        console.warn('SetDOBFieldsFunction error:', e);
                    }}
                }})();
            ";

            var page = _testWebProvider.TestInfraFunctions.GetContext().Pages.First();
            await page.EvaluateAsync(js);

            _logger.LogInformation("SetDOBFieldsFunction execution completed.");
            return FormulaValue.New(true);
        }
    }
}
