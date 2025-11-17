# Finance & Operations Portal Provider – Implementation Plan

## 1. Objective & Scope
- Deliver a production-ready `fno.portal` web provider that enables Test Engine users to automate Dynamics 365 Finance & Operations portal experiences using Power Fx test plans.
- Ensure reliable login automation, basic UI interaction parity, and complete documentation/packaging for release.

## 2. Guiding Principles
- **Iterative delivery:** Each phase closes with a verifiable increment (tests, docs, samples).
- **Parity-first:** Match existing provider capabilities (login, object model, property access) before pursuing advanced scenarios.
- **Quality gates:** Maintain passing unit/integration tests, clean builds, and security approvals throughout.

## 3. Phased Execution

### Phase 0 – Foundation (Day 0–2)
- Validate project wiring (solution references, MEF allow-list, packaging scripts).
- Secure sandbox access, storage-state artifacts, and approved test personas.
- Align stakeholders on acceptance criteria and exit definition for the provider.

### Phase 1 – Minimal Login Path (Day 3–6)
- Implement end-to-end authentication using the administrator account (`administrator@contosoax7.onmicrosoft.com`) as the baseline persona.
- Harden `GenerateTestUrl`, domain handling, and `TestEngineReady` logic for F&O hosts.
- Finalize sample `fo-login-test.yaml` and quick-start CLI documentation.
- Add CI smoke test verifying YAML-driven login using mocked Playwright context.

### Phase 2 – DOM Helper & Object Model (Day 7–12)
- Reintroduce DOM helper behind feature flag to map login/dashboard controls.
- Implement `LoadObjectModelAsync`, `GetPropertyValue`, and `SelectControl` with full error handling.
- Cover helper parsing and provider APIs with targeted unit tests.

### Phase 3 – Interaction Depth & Stability (Day 13–18)
- Add `SetProperty`, `GetItemCount`, and event-trigger support for key control types.
- Implement busy/idle detection using F&O-specific progress indicators.
- Expand YAML scenarios (success/failure assertions) leveraging new control bindings.

### Phase 4 – Release Readiness (Day 19–22)
- Create Playwright-backed integration tests executing login + navigation in CI.
- Update packaging artifacts, MEF allow-list entries, and release notes.
- Refresh provider documentation and produce adoption guidance for consumers/support.

## 4. Risks & Mitigations
| Risk | Mitigation |
| --- | --- |
| Sandbox instability or credential drift | Automate environment validation in CI; maintain contact for sandbox owners. |
| Compliance concerns with DOM helper access to PII | Run security review early; ship helper behind opt-in flag until approved. |
| Dependency conflicts (e.g., System.Text.Json warnings) | Track during builds, resolve before release milestone. |

## 5. Success Metrics
- CLI login smoke test (storage-state persona) passes nightly.
- Provider APIs achieve ≥80% unit test coverage with zero high-severity bugs.
- Documentation and samples allow teams to run `fo-login-test.yaml` without manual tweaks.

## 6. Next Actions
1. Confirm sandbox credentials and storage-state availability.
2. Kick off Phase 1 tasks and schedule weekly stakeholder syncs.
3. Log compliance review request for the DOM helper feature flag.
