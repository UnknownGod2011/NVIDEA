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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation now closes the agent-owned page before returning cancellation. Canonical page admission covers initial pages, popup adoption, fallback selection, snapshots and download sources. Credential-popup evidence uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup. Risk keyword matching uses lexical term/phrase boundaries to reduce approval fatigue. Expanded credential classification covers OTP/verification codes, PINs, recovery material, access/refresh tokens and identity-number labels. Standard HTML autocomplete security/payment tokens are classified for locator-based actions.

### 2026-09-18 — observed form-metadata safety boundary (latest run)
Completed:
- Re-read this ledger, `BrowserContracts`, `BrowserSafetyPolicy`, and the production Playwright DOM observation path before mutation.
- Extended `BrowserElement` with optional `InputType` and `AutoComplete` fields. These are explicitly non-secret form semantics and preserve source compatibility through optional trailing parameters.
- Updated `BrowserSafetyPolicy` so accessibility-reference actions consume the observed form metadata in addition to visible name/role/value. This means a policy decision can fail closed on a semantically sensitive control even when its human-facing label is innocuous.
- Added deterministic regression coverage proving accessibility-ref typing is blocked for `type=password`, `current-password`, `one-time-code`, `cc-number`, and `cc-csc`; benign `email`, `username`, `organization`, and `street-address` remain medium risk; and sensitive classification does not require a field value to be present.
- Verified repository metadata immediately before every mutation; the target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserContracts.cs`
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserObservedFormMetadataSafetyTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms observed metadata is used only for accessibility-ref target classification and no new secret value channel was introduced.
- Tests are deterministic and provider-free, but no executable .NET PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- `BrowserElement` can now represent input type/autocomplete without storing a secret value. The policy consumes these semantics for accessibility-ref actions.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking, and emergency cancellation remain intact.
- Emergency cancellation cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- The production Playwright DOM snapshot does not yet populate the new `InputType`/`AutoComplete` properties. Therefore the policy path and contract are implemented/tested, but real browser accessibility-ref observations do not yet receive this metadata. Do not claim the end-to-end gap is closed until the observer is wired and tested.
- When wiring the observer, normalize metadata and never expose input values merely because a field has an unfamiliar type/autocomplete token. Password values must remain suppressed.
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium.
- Risk matching deliberately avoids stemming/broad substring matching to prevent approval fatigue.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Wire the production `PlaywrightBrowserDriver` DOM snapshot to populate normalized `InputType` and `AutoComplete` metadata while preserving password-value suppression, then add a hermetic real-observation fixture proving an innocuously labelled password/OTP/payment field becomes blocked through its accessibility reference. After that, execute the complete opt-in Chromium boundary suite on Windows/.NET 8 when available.
