// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Provider for Dynamics 365 Finance & Operations portals.
    /// </summary>
    [Export(typeof(ITestWebProvider))]
    public class FnoPortalProvider : ITestWebProvider
    {
        private const string HelperInitExpression = "typeof window.FnoTestEngine !== 'undefined'";
        private const string HelperScript = """
(() => {
    if (typeof window === "undefined" || window.FnoTestEngine) {
        return;
    }

    const ATTRIBUTE_PRIORITY = ["data-test-id", "data-testid", "data-automation-id", "aria-label", "name", "id"];
    const BUSY_SELECTORS = [
        "d365-progress",
        "d365-progress-bar",
        "d365-spinner",
        "office-progress-indicator",
        ".fxs-busy",
        ".ms-Spinner",
        ".loading-indicator"
    ];

    const controlMap = new Map();

    function toArray(nodeList) {
        return Array.prototype.slice.call(nodeList || []);
    }

    function isVisible(element) {
        if (!element) {
            return false;
        }
        if (element.offsetParent !== null) {
            return true;
        }
        const rects = element.getClientRects();
        for (let i = 0; i < rects.length; i++) {
            const rect = rects[i];
            if (rect.width > 0 && rect.height > 0) {
                return true;
            }
        }
        return false;
    }

    function escapeAttribute(value) {
        return JSON.stringify(value);
    }

    function computeSelector(element) {
        for (const attribute of ATTRIBUTE_PRIORITY) {
            const attributeValue = element.getAttribute && element.getAttribute(attribute);
            if (attributeValue) {
                return `[${attribute}=${escapeAttribute(attributeValue)}]`;
            }
        }

        if (element.id) {
            return `#${element.id.replace(/([:.[\\])/g, "\\$1")}`;
        }

        if (!element.parentElement) {
            return element.tagName ? element.tagName.toLowerCase() : 'div';
        }

        const parentChildren = toArray(element.parentElement.children);
        const index = parentChildren.indexOf(element) + 1;
        const tagName = element.tagName ? element.tagName.toLowerCase() : 'div';
        return `${tagName}:nth-of-type(${index})`;
    }

    function computeName(element, index) {
        for (const attribute of ATTRIBUTE_PRIORITY) {
            const attributeValue = element.getAttribute && element.getAttribute(attribute);
            if (attributeValue) {
                return attributeValue.trim();
            }
        }

        if (element.id) {
            return element.id;
        }

        if (element.name) {
            return element.name;
        }

        const tagName = element.tagName ? element.tagName.toLowerCase() : 'element';
        return `${tagName}-${index}`;
    }

    function buildPropertyMetadata(element) {
        const properties = [
            { propertyName: 'Text', propertyType: 's' },
            { propertyName: 'Value', propertyType: 's' },
            { propertyName: 'Visible', propertyType: 'b' },
            { propertyName: 'Disabled', propertyType: 'b' }
        ];

        const tagName = element.tagName ? element.tagName.toLowerCase() : '';
        const inputType = element.type ? element.type.toLowerCase() : '';

        if (tagName === 'input' && (inputType === 'checkbox' || inputType === 'radio')) {
            properties.push({ propertyName: 'Checked', propertyType: 'b' });
        }

        if (tagName === 'table' || tagName === 'tbody') {
            properties.push({ propertyName: 'RowCount', propertyType: 'n' });
        }

        return properties;
    }

    function snapshotControls() {
        controlMap.clear();

        const candidates = document.querySelectorAll('[data-test-id], [data-testid], [data-automation-id], [aria-label], [role="button"], [role="textbox"], input, button, select, textarea, a');
        const controls = [];
        const seenNames = new Set();
        let index = 0;

        toArray(candidates).forEach(element => {
            if (!element) {
                return;
            }

            const name = computeName(element, index++);
            if (!name || seenNames.has(name)) {
                return;
            }

            const selector = computeSelector(element);
            controlMap.set(name, { selector });
            controls.push({ name, properties: buildPropertyMetadata(element) });
            seenNames.add(name);
        });

        return controls;
    }

    function resolveElement(itemPath) {
        if (!itemPath || !itemPath.controlName) {
            return null;
        }

        const entry = controlMap.get(itemPath.controlName);
        if (!entry) {
            return null;
        }

        try {
            return document.querySelector(entry.selector);
        } catch (error) {
            return null;
        }
    }

    function normalizePropertyName(propertyName) {
        if (!propertyName) {
            return 'value';
        }

        return propertyName.toLowerCase();
    }

    function toPropertyPayload(value) {
        if (value === undefined) {
            return null;
        }

        return value;
    }

    function getPropertyValue(itemPath) {
        const element = resolveElement(itemPath);
        if (!element) {
            return JSON.stringify({ PropertyValue: null });
        }

        const propertyName = normalizePropertyName(itemPath.propertyName);
        let value = null;

        switch (propertyName) {
            case 'text':
                value = element.innerText || element.textContent || '';
                break;
            case 'value':
                value = element.value ?? element.textContent ?? '';
                break;
            case 'visible':
                value = isVisible(element);
                break;
            case 'disabled':
                value = !!element.disabled;
                break;
            case 'checked':
                value = !!element.checked;
                break;
            case 'rowcount':
                value = element.rows ? element.rows.length : (element.children ? element.children.length : 0);
                break;
            default:
                if (propertyName in element) {
                    value = element[propertyName];
                } else if (itemPath.propertyName) {
                    value = element.getAttribute(itemPath.propertyName);
                }
                break;
        }

        return JSON.stringify({ PropertyValue: toPropertyPayload(value) });
    }

    function setPropertyValue(itemPath, value) {
        const element = resolveElement(itemPath);
        if (!element) {
            return false;
        }

        const propertyName = normalizePropertyName(itemPath.propertyName);
        const triggerChange = () => {
            element.dispatchEvent(new Event('input', { bubbles: true }));
            element.dispatchEvent(new Event('change', { bubbles: true }));
        };

        switch (propertyName) {
            case 'text':
                element.textContent = value ?? '';
                triggerChange();
                return true;
            case 'value':
                element.value = value ?? '';
                triggerChange();
                return true;
            case 'checked':
                element.checked = !!value;
                triggerChange();
                return true;
            case 'disabled':
                element.disabled = !!value;
                return true;
            default:
                if (propertyName in element) {
                    element[propertyName] = value;
                    triggerChange();
                    return true;
                }

                if (itemPath.propertyName) {
                    element.setAttribute(itemPath.propertyName, value);
                    triggerChange();
                    return true;
                }
                return false;
        }
    }

    function selectControl(itemPath) {
        const element = resolveElement(itemPath);
        if (!element) {
            return false;
        }

        element.focus();
        element.click();
        return true;
    }

    function getItemCount(itemPath) {
        const element = resolveElement(itemPath);
        if (!element) {
            return 0;
        }

        if (element.options) {
            return element.options.length;
        }

        if (element.children) {
            return element.children.length;
        }

        return 0;
    }

    function getIdleStatus() {
        if (document.readyState !== 'complete') {
            return 'Loading';
        }

        const anyBusy = BUSY_SELECTORS.some(selector => {
            try {
                const matches = document.querySelectorAll(selector);
                return toArray(matches).some(isVisible);
            } catch (error) {
                return false;
            }
        });

        return anyBusy ? 'Loading' : 'Idle';
    }

    window.FnoTestEngine = {
        buildObjectModel: () => JSON.stringify({ controls: snapshotControls() }),
        getPropertyValue,
        setPropertyValue,
        selectControl,
        getItemCount,
        getIdleStatus,
        ensureReady: () => true
    };
})();
""";

        private readonly TypeMapping _typeMapping = new TypeMapping();
        private const string ObjectModelErrorMessage = "Test Engine could not load the F&O object model in time.";
        private const string PropertyValueErrorMessage = "Test Engine could not read the requested F&O property.";
        private const string ItemCountErrorMessage = "Test Engine could not determine the number of child items for the requested F&O control.";

        public FnoPortalProvider()
        {
            ProviderState = new FnoPortalProviderState();
        }

        public ITestInfraFunctions? TestInfraFunctions { get; set; }

        public ISingleTestInstanceState? SingleTestInstanceState { get; set; }

        public ITestState? TestState { get; set; }

        public ITestProviderState? ProviderState { get; set; }

        public string[] Namespaces => new[] { "Preview" };

        public string Name => "fno.portal";

        public string CheckTestEngineObject => HelperInitExpression;

        public bool ProviderExecute => false;

        private ILogger? Logger => SingleTestInstanceState?.GetLogger();

        private ILogger GetRequiredLogger()
        {
            if (Logger == null)
            {
                throw new InvalidOperationException("Logger has not been set.");
            }

            return Logger;
        }

        private void ValidateInfrastructure()
        {
            if (TestInfraFunctions == null)
            {
                throw new InvalidOperationException("Test infrastructure has not been set.");
            }

            if (SingleTestInstanceState == null)
            {
                throw new InvalidOperationException("Single test instance state has not been set.");
            }

            if (TestState == null)
            {
                throw new InvalidOperationException("Test state has not been set.");
            }
        }

        private async Task EnsureHelperScriptAsync()
        {
            ValidateInfrastructure();

            if (!await TestInfraFunctions!.RunJavascriptAsync<bool>(HelperInitExpression))
            {
                await TestInfraFunctions.AddScriptContentAsync(HelperScript);
                await TestInfraFunctions.RunJavascriptAsync<bool>(HelperInitExpression);
            }
        }

        private void ValidateItemPath(ItemPath itemPath, bool requirePropertyName)
        {
            if (itemPath == null)
            {
                throw new ArgumentNullException(nameof(itemPath));
            }

            if (string.IsNullOrWhiteSpace(itemPath.ControlName))
            {
                Logger?.LogError("ItemPath control name is required.");
                throw new ArgumentNullException(nameof(itemPath.ControlName));
            }

            if (requirePropertyName || itemPath.Index.HasValue)
            {
                if (string.IsNullOrWhiteSpace(itemPath.PropertyName))
                {
                    Logger?.LogError("ItemPath property name is required when accessing indexed data or when explicitly requested.");
                    throw new ArgumentNullException(nameof(itemPath.PropertyName));
                }
            }

            if (itemPath.ParentControl != null)
            {
                ValidateItemPath(itemPath.ParentControl, false);
            }
        }

        public async Task CheckProviderAsync()
        {
            try
            {
                await EnsureHelperScriptAsync();

                await PollingHelper.PollAsync(
                    false,
                    isReady => !isReady,
                    async () => await TestInfraFunctions!.RunJavascriptAsync<bool>("window.FnoTestEngine.ensureReady()"),
                    TestState!.GetTimeout(),
                    GetRequiredLogger(),
                    "Timed out while initializing the F&O provider helper script.");
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                throw;
            }
        }

        public async Task<bool> CheckIsIdleAsync()
        {
            try
            {
                await EnsureHelperScriptAsync();
                var status = await TestInfraFunctions!.RunJavascriptAsync<string>("window.FnoTestEngine.getIdleStatus()");
                return string.Equals(status, "Idle", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                return false;
            }
        }

        public async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsync()
        {
            var controls = new Dictionary<string, ControlRecordValue>(StringComparer.OrdinalIgnoreCase);

            await PollingHelper.PollAsync(
                controls,
                collection => collection.Count == 0,
                async current => await LoadObjectModelAsyncHelper(current),
                TestState!.GetTimeout(),
                GetRequiredLogger(),
                ObjectModelErrorMessage);

            Logger?.LogDebug($"Loaded {controls.Count} controls from F&O portal.");
            return controls;
        }

        private async Task<Dictionary<string, ControlRecordValue>> LoadObjectModelAsyncHelper(Dictionary<string, ControlRecordValue> controls)
        {
            try
            {
                await EnsureHelperScriptAsync();
                var objectModelJson = await TestInfraFunctions!.RunJavascriptAsync<string>("window.FnoTestEngine.buildObjectModel()");

                if (string.IsNullOrWhiteSpace(objectModelJson))
                {
                    return controls;
                }

                var objectModel = JsonConvert.DeserializeObject<JSObjectModel>(objectModelJson);
                if (objectModel?.Controls == null)
                {
                    return controls;
                }

                foreach (var control in objectModel.Controls)
                {
                    if (string.IsNullOrWhiteSpace(control.Name) || controls.ContainsKey(control.Name))
                    {
                        continue;
                    }

                    var controlType = RecordType.Empty();
                    var skippedProperties = new List<string>();

                    if (control.Properties != null)
                    {
                        foreach (var property in control.Properties)
                        {
                            if (string.IsNullOrWhiteSpace(property.PropertyName))
                            {
                                continue;
                            }

                            if (_typeMapping.TryGetType(property.PropertyType, out var formulaType))
                            {
                                controlType = controlType.Add(property.PropertyName, formulaType);
                            }
                            else
                            {
                                skippedProperties.Add(property.PropertyName);
                            }
                        }
                    }

                    if (skippedProperties.Count > 0)
                    {
                        Logger?.LogTrace($"Skipped unsupported properties for {control.Name}: {string.Join(", ", skippedProperties)}");
                    }

                    var controlValue = new ControlRecordValue(controlType, this, control.Name);
                    controls.Add(control.Name, controlValue);
                }

                if (ProviderState is FnoPortalProviderState state)
                {
                    state.UpdateControlNames(controls.Keys);
                }

                return controls;
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                throw;
            }
        }

        public T GetPropertyValueFromControl<T>(ItemPath itemPath)
        {
            try
            {
                var awaiter = GetPropertyValueFromControlAsync<T>(itemPath).GetAwaiter();
                PollingHelper.Poll(awaiter, waiter => !waiter.IsCompleted, null, TestState!.GetTimeout(), GetRequiredLogger(), PropertyValueErrorMessage);
                return awaiter.GetResult();
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                throw;
            }
        }

        private async Task<T> GetPropertyValueFromControlAsync<T>(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, true);
                await EnsureHelperScriptAsync();
                var itemPathJson = JsonConvert.SerializeObject(itemPath);
                var result = await TestInfraFunctions!.RunJavascriptAsync<string>($"window.FnoTestEngine.getPropertyValue({itemPathJson})");
                return (T)(object)result;
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, GetRequiredLogger());
                throw;
            }
        }

        public async Task<bool> SelectControlAsync(ItemPath itemPath, string filePath = null)
        {
            try
            {
                ValidateItemPath(itemPath, false);
                await EnsureHelperScriptAsync();
                var itemPathJson = JsonConvert.SerializeObject(itemPath);
                return await TestInfraFunctions!.RunJavascriptAsync<bool>($"window.FnoTestEngine.selectControl({itemPathJson})");
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, GetRequiredLogger());
                throw;
            }
        }

        public async Task<bool> SetPropertyAsync(ItemPath itemPath, FormulaValue value)
        {
            try
            {
                ValidateItemPath(itemPath, false);
                await EnsureHelperScriptAsync();
                var payload = JsonConvert.SerializeObject(value?.ToObject());
                var itemPathJson = JsonConvert.SerializeObject(itemPath);
                await TestInfraFunctions!.RunJavascriptAsync<bool>($"window.FnoTestEngine.setPropertyValue({itemPathJson}, {payload})");
                return true;
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, GetRequiredLogger());
                throw;
            }
        }

        public int GetItemCount(ItemPath itemPath)
        {
            var awaiter = GetItemCountAsync(itemPath).GetAwaiter();
            PollingHelper.Poll(awaiter, waiter => !waiter.IsCompleted, null, TestState!.GetTimeout(), GetRequiredLogger(), ItemCountErrorMessage);
            return awaiter.GetResult();
        }

        private async Task<int> GetItemCountAsync(ItemPath itemPath)
        {
            try
            {
                ValidateItemPath(itemPath, false);
                await EnsureHelperScriptAsync();
                var itemPathJson = JsonConvert.SerializeObject(itemPath);
                return await TestInfraFunctions!.RunJavascriptAsync<int>($"window.FnoTestEngine.getItemCount({itemPathJson})");
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                ExceptionHandlingHelper.CheckIfOutDatedPublishedApp(ex, Logger);
                throw;
            }
        }

        public async Task<object> GetDebugInfo()
        {
            await EnsureHelperScriptAsync();

            var debugInfo = new Dictionary<string, object>
            {
                ["PageCount"] = TestInfraFunctions!.GetContext().Pages.Count,
                ["Domain"] = TestState!.GetDomain(),
                ["LoadedControls"] = ProviderState is FnoPortalProviderState state ? state.ControlNames.Count : 0,
                ["Idle"] = await CheckIsIdleAsync()
            };

            return debugInfo;
        }

        public async Task<bool> TestEngineReady()
        {
            try
            {
                await EnsureHelperScriptAsync();
                return await TestInfraFunctions!.RunJavascriptAsync<bool>(HelperInitExpression);
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex.ToString());
                throw;
            }
        }

        public string GenerateTestUrl(string domain, string additionalQueryParams)
        {
            ValidateInfrastructure();

            if (string.IsNullOrWhiteSpace(domain))
            {
                var environment = TestState!.GetEnvironment();
                if (string.IsNullOrWhiteSpace(environment))
                {
                    throw new InvalidOperationException("Environment must be provided when no domain is supplied.");
                }

                domain = environment.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? environment
                    : $"https://{environment.TrimEnd('/')}.operations.dynamics.com";
            }

            var builder = new UriBuilder(domain);
            var queryBuilder = new StringBuilder();

            void appendQuery(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return;
                }

                if (queryBuilder.Length > 0)
                {
                    queryBuilder.Append('&');
                }

                queryBuilder.Append(query.TrimStart('?'));
            }

            appendQuery(builder.Query);
            appendQuery(additionalQueryParams);

            if (queryBuilder.ToString().IndexOf("source=testengine", StringComparison.OrdinalIgnoreCase) < 0)
            {
                appendQuery("source=testengine");
            }

            builder.Query = queryBuilder.ToString();
            var uri = builder.Uri;

            TestState!.SetDomain($"{uri.Scheme}://{uri.Authority}");
            return uri.ToString();
        }
    }

    internal class FnoPortalProviderState : ITestProviderState
    {
        public HashSet<string> ControlNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void UpdateControlNames(IEnumerable<string> names)
        {
            ControlNames.Clear();
            foreach (var name in names)
            {
                ControlNames.Add(name);
            }
        }

        public object GetState()
        {
            return new
            {
                controlNames = ControlNames.ToArray()
            };
        }
    }
}
