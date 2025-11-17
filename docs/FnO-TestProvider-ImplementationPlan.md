# Finance & Operations (F&O) Provider Implementation Plan

---

## Executive Summary

### Plan at a Glance

- **Phase 1 (Weeks 1–2):** Establish authentication foundation, certificate reuse, and provider scaffold needed to reach an authenticated landing page.
- **Phase 2 (Weeks 3–4):** Light-up navigation helpers, control discovery, and initial Power Fx bindings on top of the authenticated session.
- **Phase 3 (Weeks 5–6):** Expand diagnostics, workspace tooling, and advanced control behaviors while hardening resiliency.
- **Phase 4 (Weeks 7–8):** Drive end-to-end validation, documentation, and performance polish.

This plan describes how we will introduce a Finance & Operations (F&O) provider into the PowerApps Test Engine. The provider expands automated test coverage to Dynamics 365 F&O workloads by integrating with the existing Playwright-based test infrastructure, Power Fx extensibility, and storage-state authentication flows already used across the project.

Key objectives:

- Deliver an extensible provider that plugs into the current `ITestWebProvider` abstraction alongside the Canvas and Model-Driven App providers.
- Offer F&O-aware navigation, control discovery, and Power Fx helpers that let test authors express scenarios using familiar Test Engine patterns.
- Provide a test and diagnostics harness that ensures quality, debuggability, and parity with other first-party providers.

---

## Architecture Snapshot

| Component | Purpose | Repo Location |
|-----------|---------|---------------|
| `testengine.provider.fno` | Primary implementation assembly for provider, controls, functions, and configuration. | `src/testengine.provider.fno/` (new) |
| `Microsoft.PowerApps.TestEngine` | Core abstractions such as `ITestWebProvider`, `ITestInfraFunctions`, and shared state objects. | `src/Microsoft.PowerApps.TestEngine/` |
| `testengine.provider.canvas` & `testengine.provider.mda` | Reference providers illustrating expected patterns (MEF exports, Power Fx wiring). | `src/testengine.provider.canvas/`, `src/testengine.provider.mda/` |
| `testengine.provider.fno.tests` | Dedicated test project mirroring structure of other provider test suites. | `src/testengine.provider.fno.tests/` (new) |

### Integration Points

The F&O provider must implement the following abstractions:

- `ITestWebProvider` — primary entry point for provider-specific navigation and control resolution.
- `ITestInfraFunctions` — Playwright automation helpers used to manipulate the browser.
- `ITestState` and `ISingleTestInstanceState` — shared state used across the engine and per-test execution context.
- `IPowerFxEngine` — for registering F&O-specific Power Fx functions that surface to test authors.

### F&O-Specific Considerations

- **URL structure:** F&O environments follow `https://<env>.operations.dynamics.com/?origin=discovery&cmp=<company>` and leverage workspaces. URL helpers must understand company, legal entity, and workspace context.
- **Authentication:** F&O can require redirects, Azure AD B2E, or cloud-hosted login flows. The provider must support storage-state reuse and rehydration via existing Test Engine mechanisms.
- **Object model:** F&O UI differs from Canvas or MDA—forms, grids, dialogs, infologs, and workspace navigation require specialized selectors and interactions.
- **Navigation:** Workspace modules, dashboards, and menus demand a navigator capable of drilling into target areas and switching companies.

---

## Implementation Phases

| Phase | Duration | Focus | Key Deliverables |
|-------|----------|-------|------------------|
| 1. Authentication & Scaffold | Weeks 1–2 | Certificate-backed auth reuse, storage-state integration, provider skeleton | `FnoTestWebProvider` shell, certificate integration, authenticated smoke login |
| 2. Navigation & Controls | Weeks 3–4 | URL/navigation helpers, control discovery, Power Fx registration | Navigation utilities, form/grid control classes, initial Power Fx functions & unit tests |
| 3. Diagnostics & Resiliency | Weeks 5–6 | Advanced control behaviors, diagnostics, workspace tooling | Error handling services, infolog detection, workspace navigator, telemetry hooks |
| 4. Testing & Polish | Weeks 7–8 | Comprehensive validation, docs, perf | Full test suite, docs in `docs/`, perf improvements |

Each phase culminates in a demo or review checkpoint with the lead to validate progress.

---

## Weekly Work Breakdown

| Week | Phase Alignment | Primary Work Items |
|------|-----------------|--------------------|
| 1 | Phase 1 | Configure certificate-backed storage state in `StorageStateUserManager`, hook into existing personas, and prototype authenticated page load via Playwright. |
| 2 | Phase 1 | Finalize provider scaffolding (`FnoTestWebProvider`), wire up MEF exports, implement fallback interactive login logic, and validate smoke login using `samples/fno/login.fx.yaml`. |
| 3 | Phase 2 | Build form/grid control abstractions, codify selector strategy, and prototype workspace navigator helpers. |
| 4 | Phase 2 | Register Power Fx functions, author unit tests around control discovery/mapping, and document control naming conventions. |
| 5 | Phase 3 | Deliver workspace navigator helpers, advanced grid/form operations, and begin resiliency sweeps for long-running actions. |
| 6 | Phase 3 | Implement diagnostics/error handling, integrate infolog detection, and add telemetry instrumentation for failure triage. |
| 7 | Phase 4 | Expand automated coverage (unit + integration), validate CI wiring, and draft provider guidance in `docs/`. |
| 8 | Phase 4 | Execute performance tuning, finalize documentation pack, walk through sign-off checklist with leads. |

---

## Key Design Highlights

- **Provider foundation:** Implement `FnoTestWebProvider` under `src/testengine.provider.fno/Providers/` with MEF exports so the engine can resolve the provider by name (`fno`).
- **Navigation & URLs:** Generate F&O-specific URLs (domain + company + workspace) and reuse existing `ITestInfraFunctions` helpers for navigation waits, redirect handling, and storage-state replay.
- **Control abstraction:** Introduce form, grid, dialog, and workspace helpers in the `Controls/` folder to wrap common selectors and reduce raw Playwright usage in tests.
- **Authentication pipeline:** Extend the authentication provider to detect F&O login state, execute company selection, and integrate with `StorageStateUserManager` for reuse across executions.
	- Today the engine persists auth state via the Dataverse-backed storage-state user manager, which secures secrets using certificate-based data protection (`testengine.user.storagestate`).
	- Extend this certificate flow so the new provider can reuse existing personas and only fall back to interactive flows when storage state is stale.
- **Diagnostics:** Add lightweight error detection and diagnostics hooks so failures surface company/workspace context and infolog data in existing artifacts.

---

## Configuration & Settings

- New `FnoProviderSettings` class (default company, workspace, timeouts, module mappings) loaded via Test Engine configuration pipeline.
- `testSettings.extensionModules.fno` block (see `samples/fno/login.fx.yaml`) to override defaults per test suite.
- Optional `ModuleMappings` dictionary to translate short codes (e.g., `GL`) to full module names.

---

## Test & Validation Strategy

- Create `testengine.provider.fno.tests` project mirroring other providers with unit coverage for URL generation, navigation, authentication, and control accessors.
- Reuse existing Playwright fixtures for integration scenarios against a secured F&O sandbox (gated via environment variables).
- Add regression tests for selector integrity and error detection logic as new controls are introduced.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Authentication differences per tenant | Login failures or flakes | Keep authentication handlers modular, rely on storage-state caching, document Azure AD prerequisites. |
| DOM/selector churn across releases | Broken automation | Centralize selectors in the control mapper, add selector regression tests, monitor F&O release cadence. |
| Large-environment performance | Slow tests | Cache workspace metadata, tune navigation waits, offer configuration knobs for timeouts. |

---

## Reference Points

- Existing providers under `src/testengine.provider.*` for MEF and Power Fx patterns.
- Power Fx guidelines in `docs/PowerFX/`.
- Sample scenarios already planted in `samples/fno/`.
- Pipeline integration details in `azure-pipelines.yml`.
