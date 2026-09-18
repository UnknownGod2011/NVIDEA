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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation closes the agent-owned page before returning cancellation. Canonical page admission covers initial pages, popup adoption, fallback selection, snapshots and download sources. Credential-popup evidence uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup. Risk keyword matching uses lexical term/phrase boundaries to reduce approval fatigue. Expanded credential classification covers OTP/verification codes, PINs, recovery material, access/refresh tokens, identity-number labels and standard HTML autocomplete security/payment tokens. Real Playwright observations now carry bounded input-type/autocomplete semantics without adding a new secret metadata channel; password values remain suppressed.

### 2026-09-18 — observed-value privacy hardening (latest run)
Completed:
- Re-read this ledger, production Playwright observation code, the observed-value privacy classifier and its tests before mutation.
- Refactored `BrowserObservedValuePrivacyPolicy` so its sensitive HTML autocomplete tokens are published as a read-only canonical list while classification uses a case-insensitive set built from the same source. This gives the browser-side snapshot implementation one authoritative token contract instead of requiring a second hand-maintained list.
- Added contract coverage proving every published token is normalized, unique, suppresses values directly, and still suppresses when preceded by legal HTML autocomplete qualifiers such as `section-checkout billing`.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserObservedValuePrivacyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserObservedValuePrivacyPolicyTests.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms the public contract contains only non-secret HTML field-type tokens and remains read-only to callers.
- Deterministic xUnit coverage was added, but no executable .NET PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Observed-value privacy classification is metadata-only and token-bounded; the canonical token list now prevents policy/browser-observer drift once browser-side suppression consumes it.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking, and emergency cancellation remain intact.
- Emergency cancellation cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- `BrowserObservedValuePrivacyPolicy` is not yet invoked before the Playwright DOM snapshot reads a field value. Until that wiring lands, non-password OTP/payment fields may still expose their current value through generic `BrowserElement.Value`; do not claim that privacy gap closed yet.
- The production metadata path still needs a hermetic real-Chromium observation fixture proving innocuously labelled password/OTP/payment fields are observed with expected metadata, sensitive values are absent, and actions are blocked through generated accessibility references.
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium.
- Risk matching deliberately avoids stemming/broad substring matching to prevent approval fatigue.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Pass `BrowserObservedValuePrivacyPolicy.SensitiveAutocompleteTokens` into the Playwright `EvaluateAsync` snapshot and decide suppression from input type/autocomplete before reading `el.value`, then add a hermetic real-Chromium fixture proving innocuously labelled password/OTP/payment controls retain non-secret metadata while their values are absent and accessibility-reference typing is fail-closed.
