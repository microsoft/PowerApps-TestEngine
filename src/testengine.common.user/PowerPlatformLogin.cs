using Microsoft.Playwright;

namespace testengine.common.user
{
    public class PowerPlatformLogin
    {
        public static string EmailSelector = "input[type=\"email\"]";

        public static string ERROR_DIALOG_KEY = "ErrorDialogTitle";

        public static string DEFAULT_OFFICE_365_CHECK = "var element = document.getElementById('O365_MainLink_Settings'); if (typeof(element) != 'undefined' && element != null) { 'Idle' } else { 'Loading' }";
        public static string DIAGLOG_CHECK_JAVASCRIPT = "var element = document.querySelector('.ms-Dialog-title, #ErrorTitle, .NotificationTitle'); if (typeof(element) != 'undefined' && element != null) { element.textContent.trim() } else { '' }";

        public Func<IPage, Task<bool>> LoginIsComplete { get; set; } 

        public PowerPlatformLogin()
        {
            // Use the default check that the login process is idle, caller could override that behaviour with any additional checks
            LoginIsComplete = CheckIsIdleAsync;
        }

        public virtual async Task HandleCommonLoginState(LoginState state) {
            
            // Error Checks - Power Apps Scenarios
            //TODO: Verify App not shared
            //TODO: Handle unlicenced
            //TODO: DLP Violation
            //TODO: No dataverse access rights (MDA)
            var title = await DialogTitle(state.Page);
            if (!string.IsNullOrEmpty(title))
            {
                if (!state.Module.Settings.ContainsKey(ERROR_DIALOG_KEY))
                {
                    state.Module.Settings.TryAdd(ERROR_DIALOG_KEY, title);
                } else
                {
                    state.Module.Settings[ERROR_DIALOG_KEY] = title;
                }

                state.IsError = true;

                if (state.CallbackErrorFound != null)
                {
                    await state.CallbackErrorFound();
                }                
            }

            var url = state.Page.Url;

            // Remove any redirect added by Microsoft Cloud for Web Apps so we get the desired url
            url = url?.Replace(".mcas.ms", "");

            // Remove home location, required for Portal Providers
            url = url?.Replace("/home", "");

            // Need to check if page is idle to avoid case where we can get race condition before redirect to login
            if (url.IndexOf(state.DesiredUrl) >= 0 && await LoginIsComplete(state.Page) && !state.IsError)
            {
                if (state.CallbackDesiredUrlFound != null)
                {
                    await state.CallbackDesiredUrlFound(state.DesiredUrl);
                }
                
                state.FoundMatch = true;
                state.MatchHost = new Uri(state.Page.Url).Host;
            }

            if (!(state.Page.Url.IndexOf(state.DesiredUrl) >= 0) && !state.IsError)
            {
                if (state.Page.Url != "about:blank")
                {
                    // Default the user into the dialog if it is visible
                    await HandleUserEmailScreen(EmailSelector, state);

                    // Next user could be presented with password
                    // Could also be presented with others configured MFA options
                }
            }
        }

        /// <summary>
        /// Attempts to complete the user email as part of the login process if it is known
        /// </summary>
        /// <param name="selector">The selector to fid the email</param>
        /// <param name="state">The current login session state</param>
        /// <returns>Completed task</returns>
        private async Task HandleUserEmailScreen(string selector, LoginState state)
        {
            if (state.EmailHandled)
            {
                return;
            }
            try
            {
                var page = state.Page;
                if (await page.Locator(selector).IsEditableAsync() && !state.EmailHandled)
                {
                    state.EmailHandled = true;
                    await page.Locator(selector).PressSequentiallyAsync(state.UserEmail, new LocatorPressSequentiallyOptions { Delay = 50 });
                    await page.Keyboard.PressAsync("Tab", new KeyboardPressOptions { Delay = 20 });
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Check if standard post login Document Object Model elements can be found
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        private async Task<bool> CheckIsIdleAsync(IPage page)
        {
            try
            {
                return (await page.EvaluateAsync<string>(DEFAULT_OFFICE_365_CHECK)) == "Idle";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to determin if a Power Platform dialog is visible to the user. If so return the title
        /// </summary>
        /// <param name="page">The page to check</param>
        /// <returns>The located title if it exists</returns>
        private async Task<string> DialogTitle(IPage page)
        {
            try
            {
                return await page.EvaluateAsync<string>(DIAGLOG_CHECK_JAVASCRIPT);
            }
            catch
            {
                return "";
            }
        }
    }
}
