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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation closes the agent-owned page before returning cancellation. Canonical page admission covers initial pages, popup adoption, fallback selection, snapshots and download sources. Credential-popup evidence uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup. Risk keyword matching uses lexical term/phrase boundaries to reduce approval fatigue. Expanded credential classification covers OTP/verification codes, PINs, recovery material, access/refresh tokens, identity-number labels and standard HTML autocomplete security/payment tokens.

### 2026-09-18 — production observed form-metadata wiring (latest run)
Completed:
- Re-read this ledger and the production `PlaywrightBrowserDriver` observation implementation before mutation.
- Wired the real Playwright DOM snapshot to emit normalized, bounded, non-secret `InputType` and `AutoComplete` metadata for observed form controls and pass it into `BrowserElement`.
- Input type defaults to the browser-equivalent `text` semantic for `<input>` without an explicit type, is lower-cased/trimmed and bounded to 64 characters. Autocomplete metadata is lower-cased, whitespace-normalized and bounded to 128 characters.
- Preserved password-value suppression: `type=password` continues to produce `Value = null`; metadata is derived only from attributes, never from field contents.
- This closes the previous production wiring gap: accessibility-reference safety decisions can now consume DOM form semantics, allowing innocuously labelled password/OTP/payment fields to reach the existing fail-closed classification path.
- Verified repository metadata immediately before each mutation; the target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/PlaywrightBrowserDriver.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms `InputType`/`AutoComplete` flow from DOM attributes -> `DomElementSnapshot` -> `BrowserElement`.
- Static inspection confirms password values remain suppressed and metadata has explicit length bounds.
- Existing deterministic policy tests cover accessibility-ref blocking for password/current-password/one-time-code/cc-number/cc-csc and benign autocomplete controls; no executable .NET PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Real Playwright observations now carry bounded input-type/autocomplete semantics without adding a secret-value channel. Password values remain suppressed.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking, and emergency cancellation remain intact.
- Emergency cancellation cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- The production metadata path is statically wired but still needs a hermetic real-Chromium observation fixture proving an innocuously labelled password/OTP/payment field is observed with the expected metadata and blocked through its generated accessibility reference. Do not claim an end-to-end Chromium PASS until executed.
- Non-password sensitive fields (for example OTP/payment) may still have their current value represented in the generic observation `Value`; the safety policy blocks autonomous typing, but a stronger privacy posture should suppress observed values for semantically sensitive autocomplete categories as well.
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium.
- Risk matching deliberately avoids stemming/broad substring matching to prevent approval fatigue.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Strengthen observation privacy so values are suppressed not only for `type=password` but also for form controls whose normalized autocomplete metadata denotes OTP/payment/authentication secrets, while retaining their non-secret metadata for policy classification. Add deterministic mapping tests and then a hermetic real-Chromium observation fixture covering innocuous labels and accessibility-reference enforcement.
