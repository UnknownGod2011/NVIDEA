# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-18 — browser safety and validation
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation closes the agent-owned page before returning cancellation. Canonical page admission covers initial pages, popup adoption, fallback selection, snapshots and download sources. Credential-popup evidence uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup. Risk keyword matching uses lexical term/phrase boundaries to reduce approval fatigue. Expanded credential classification covers OTP/verification codes, PINs, recovery material, access/refresh tokens, identity-number labels and standard HTML autocomplete security/payment tokens. Real Playwright observations carry bounded input-type/autocomplete semantics without adding a secret metadata channel.

### 2026-09-18 — observed-value privacy hardening
Completed:
- Refactored `BrowserObservedValuePrivacyPolicy` so sensitive HTML autocomplete tokens are a read-only canonical contract and the C# classifier derives from the same source.
- Added contract coverage proving every published token is normalized, unique, directly suppression-triggering and suppression-triggering with legal HTML autocomplete qualifiers.
- Wired the canonical token contract into the production Playwright DOM snapshot. Snapshot JavaScript decides suppression from bounded `input type` / `autocomplete` metadata before accessing `el.value`.
- Password, current/new-password, OTP and supported payment credential/expiry fields therefore serialize `value: null`; benign fields continue to expose bounded ordinary form values as before.
- Added `BrowserObservedValueChromiumIntegrationTests`: a hermetic, opt-in real-Chromium loopback fixture with synthetic password, OTP, card number, CVV, expiry and benign email controls. It asserts sensitive values are null, non-secret `InputType`/`AutoComplete` survive, benign values remain observable, and production `BrowserSafetyPolicy` blocks accessibility-reference typing into every sensitive control.
- Hardened that fixture for SPA-style dynamic forms: an initially benign input is mutated in-place to `autocomplete=one-time-code` with a synthetic OTP, then a fresh production observation must suppress the new value and the safety policy must block accessibility-reference typing. This guards against stale benign classifications across DOM re-renders/repurposing without navigation.
- The fixture reuses the existing `NVIDEA_RUN_BROWSER_INTEGRATION=1` opt-in convention and a temporary persistent browser profile; it performs no external network or paid-provider operation.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `tests/Nvidea.Core.Tests/BrowserObservedValueChromiumIntegrationTests.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms the fixture exercises the production `PersistentBrowserContextFactory`, `PlaywrightBrowserDriver`, repeated observation path and `BrowserSafetyPolicy`, rather than a mock observer.
- The dynamic-form case uses Playwright's label locator against a real DOM control, mutates only synthetic loopback data, and requires the subsequent production snapshot to re-evaluate current DOM semantics. Current Playwright guidance recommends user-facing locators such as labels/roles for form controls and resolves locators against the current DOM on each action.
- Existing deterministic policy/contract tests cover the canonical token vocabulary, but no executable .NET or Chromium PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values from metadata before reading the DOM value while retaining non-secret type/autocomplete semantics needed for safety decisions; the integration fixture now covers in-place semantic changes common in SPAs.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking, and emergency cancellation remain intact.
- Emergency cancellation cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- The real-Chromium observed-value privacy fixture, including its dynamic-form case, is committed but still needs execution on Windows with .NET 8, restored packages and matching Playwright Chromium; until then its PASS is unverified.
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures likewise still need execution in that environment.
- Risk matching deliberately avoids stemming/broad substring matching to prevent approval fatigue.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Run the opt-in browser integration suite on a Windows/.NET 8 environment with matching Playwright Chromium and fix any real-browser failures, prioritizing `BrowserObservedValueChromiumIntegrationTests` including the new dynamic-form case; then record exact executable evidence without weakening transport, secret-suppression, or confirmation boundaries.
