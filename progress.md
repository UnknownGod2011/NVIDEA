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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation now closes the agent-owned page before returning cancellation. Canonical page admission covers initial pages, popup adoption, fallback selection, snapshots and download sources. Credential-popup evidence uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup. Risk keyword matching uses lexical term/phrase boundaries to reduce approval fatigue. Expanded credential classification covers OTP/verification codes, PINs, recovery material, access/refresh tokens and identity-number labels.

### 2026-09-18 — HTML form semantic credential hardening (latest run)
Completed:
- Re-read this ledger and current browser policy/Playwright observation implementation before mutation.
- Added standard HTML autocomplete security/payment semantics to the deterministic sensitive-field vocabulary: `current-password`, `new-password`, `one-time-code`, `cc-number`, `cc-csc`, expiry variants, `cc-name`, `transaction-amount`, and `transaction-currency`.
- This immediately protects autonomous actions that use CSS/attribute locators even when a malicious or unusual page gives the field an innocuous/empty visible label; Type remains fail-closed rather than approval-gated.
- Added a dedicated regression suite proving sensitive autocomplete CSS locators are blocked while benign `email`, `username`, `organization`, and `street-address` autocomplete locators remain normal medium-risk form interactions.
- Verified repository metadata immediately before every mutation; `full_name` was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserFormSemanticSafetyTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the new semantic tokens pass through the existing lexical sensitive-field branch: Type => blocked; matching non-Type interaction => high risk + approval.
- New tests cover seven sensitive autocomplete CSS selectors and four benign controls. No executable .NET PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels. Standard sensitive HTML autocomplete tokens are now recognized when they appear in locators.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking, and emergency cancellation remain intact.
- Emergency cancellation cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium.
- The current autocomplete hardening sees semantic tokens only when they are represented in the action/locator text. Playwright DOM observations still do not carry trusted input `type`/`autocomplete` metadata into `BrowserElement`; an accessibility-ref action against an innocuously labelled password/OTP/payment field can therefore still depend on visible vocabulary. This is the next code-hardening gap.
- Risk matching deliberately avoids stemming/broad substring matching to prevent approval fatigue.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Extend `BrowserElement` and the Playwright DOM observation snapshot with normalized, non-secret form metadata (`input type` and `autocomplete` only), then make `BrowserSafetyPolicy` consume those trusted attributes for accessibility-ref actions. Add hermetic tests proving an innocuously labelled `type=password`, `autocomplete=one-time-code`, and payment field is blocked without exposing field values. If a Windows/.NET 8 + Chromium environment becomes available first, execute the complete opt-in browser boundary suite and treat its server-side counters as decisive evidence.
