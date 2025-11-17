# Dynamics 365 Finance & Operations Portal Provider

## Overview

The `fno.portal` provider lets the Power Apps Test Engine drive Dynamics 365 Finance & Operations (F&O) portals with the same Power Fx-based test experience used for other web surfaces. The provider injects a lightweight helper script that surfaces the portal object model, normalises control names, and adds safety checks to keep tests stable while the page finishes loading.

## Getting started

- **Command-line**: pass `--provider "fno.portal"` (or `-p fno.portal`) when launching the Test Engine executable.
- **YAML**: set `provider: fno.portal` in your `testPlan.fx.yaml` definition.
- **Environment**: if you omit `domain`, the provider derives it from the `environment` value and automatically resolves to `https://<environment>.operations.dynamics.com`. When a full domain is supplied, the provider reuses it and preserves any port number.
- **Query parameters**: a `source=testengine` flag is appended to help identify automated runs; you can append additional query parameters through the standard Test Engine configuration.

## Control discovery

The injected helper inspects the portal DOM and builds an object model that exposes each discovered element as a Power Fx record. Control names are generated deterministically using the following priority order of HTML attributes:

1. `data-test-id`
2. `data-testid`
3. `data-automation-id`
4. `aria-label`
5. `name`
6. `id`
7. Stable fallbacks based on tag name and index

Each control record includes metadata for common properties:

- `Text`
- `Value`
- `Visible`
- `Disabled`
- `Checked` (check boxes and radio buttons)
- `RowCount` (tables)

The helper caches selectors so later property or selection calls are resolved quickly.

## Supported interactions

- **Object model loading**: polls until the helper returns a non-empty control map, logging how many controls were discovered.
- **Property reads**: exposes values via `GetPropertyValue`, including visibility, disabled state, checked state, and row counts.
- **Property writes**: updates values through Power Fx assignments and fires the appropriate DOM events (`input` and `change`).
- **Selection**: focuses and clicks controls when you call `SelectControl` from Power Fx.
- **Collection size**: `GetItemCount` returns the number of options or child elements for list-like controls.
- **Idle detection**: waits for the page to reach a ready state using common F&O busy indicators (`d365-progress`, `office-progress-indicator`, `.fxs-busy`, etc.), reducing flaky timing issues.

## Example YAML snippet

```yaml
# testPlan.fx.yaml
configuration:
  environment: contoso
  # domain: https://contoso.operations.dynamics.com  # optional override
  provider: fno.portal
  appLogicalName: FinanceOperationsPortal
```

Once the configuration is in place you can interact with portal controls directly from Power Fx test steps, for example:

```powerfx
// Verify the primary contact value and update it
Assert(PrimaryContact.Text = "Adele Vance");
SetProperty(PrimaryContact, Text, "Alex Wilber");
Select(CustomerLookup);
```

## Recommendations

- Add stable automation-friendly attributes (for example `data-test-id`) to important elements to influence the generated control names.
- Prefer Power Fx `Assert`, `Select`, and `SetProperty` functions to keep tests readable and leverage provider semantics.
- Pair UI automation with mocked Dataverse calls where possible to minimise the need for live data manipulation.

## Limitations and notes

- The provider focuses on the F&O portal shell; custom iframes or embedded applications may require additional scripting.
- Only the most common properties are exposed today. Unsupported properties are logged at trace level so you can identify gaps quickly.
- Busy indicator detection is heuristic-based. If your portal uses unique loading spinners, consider extending the selector list inside the helper script.
