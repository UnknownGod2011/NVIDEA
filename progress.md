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

### 2026-09-18 — observed-value privacy classifier (latest run)
Completed:
- Re-read this ledger, the production Playwright observation implementation, and existing observed-form safety tests before mutation.
- Added `BrowserObservedValuePrivacyPolicy`, a production classifier that decides whether a form control's current value must be omitted using only non-secret `input type` and `autocomplete` metadata.
- Sensitive coverage includes password inputs, current/new passwords, one-time codes, card number/CVV and card expiry fields. HTML autocomplete qualifiers such as `section-*`, `shipping`, and `billing` are handled by exact token matching rather than substring matching.
- Added deterministic tests for sensitive semantics, qualifier-bearing autocomplete values, case normalization, benign username/email/address/card-name/card-type controls, and a near-match (`one-time-coder`) to guard against approval/privacy false positives.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserObservedValuePrivacyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserObservedValuePrivacyPolicyTests.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms the classifier never accepts or examines a field value and uses exact whitespace-delimited autocomplete tokens.
- Deterministic xUnit coverage was added, but no executable .NET PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- The new observed-value privacy classifier is deliberately metadata-only and token-bounded, avoiding secret inspection and substring false positives.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking, and emergency cancellation remain intact.
- Emergency cancellation cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- `BrowserObservedValuePrivacyPolicy` is production code with deterministic tests but is not yet invoked by the Playwright DOM snapshot. Until that wiring lands, non-password OTP/payment fields may still expose their current value through generic `BrowserElement.Value`; do not claim that privacy gap closed yet.
- The production metadata path still needs a hermetic real-Chromium observation fixture proving innocuously labelled password/OTP/payment fields are observed with expected metadata, sensitive values are absent, and actions are blocked through generated accessibility references.
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium.
- Risk matching deliberately avoids stemming/broad substring matching to prevent approval fatigue.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Wire `BrowserObservedValuePrivacyPolicy` into the Playwright observation path so sensitive values are never read/copied into `DomElementSnapshot`, then add a hermetic real-Chromium fixture proving innocuously labelled password/OTP/payment controls retain non-secret metadata while their values are absent and accessibility-reference typing is fail-closed.
