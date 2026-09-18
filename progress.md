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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation now closes the agent-owned page before returning cancellation so a Playwright operation cannot silently continue behind a stopped executor. Canonical `BrowserPageAdmissionPolicy` is integrated into the session driver so initial-page validation, popup adoption, fallback selection, snapshots and download-source validation share transport + host admission semantics.

### 2026-09-18 — credential-popup pre-dispatch evidence (latest run)
Completed:
- Re-read this ledger and the complete persistent-browser integration fixture before mutation.
- Extended `LocalSessionSite` with thread-safe per-path server-side request counters and a bounded `WaitForRequestCountAsync` helper.
- Renamed the popup integration contract to `AllowedPopup_BecomesActive_AndCredentialBearingPopup_IsBlockedBeforeDispatch` to state the stronger property being measured.
- Added a positive-control network assertion: `/allowed-popup` must be observed by the server before the test trusts popup behavior. This prevents a false security PASS caused by a dead/broken fixture.
- After attempting the credential-bearing popup on the same allowlisted loopback host, the fixture retains a 350 ms late-arrival window and requires the server-side `/credential-popup` count to remain exactly zero.
- Retained the existing page-snapshot assertions requiring all surviving pages to be permitted, credential-free, on the allowlisted host, and excluding `/credential-popup`.
- Updated the credential-popup sentinel text to describe the stronger no-dispatch invariant.
- Explicitly fetched repository metadata immediately before both mutations and verified `repository_full_name` was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `tests/Nvidea.Core.Tests/PersistentBrowserSessionIntegrationTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the server increments request counts immediately after parsing the HTTP request line, before constructing/sending any response; a destination count therefore proves network dispatch occurred.
- The allowed-popup positive control waits for count >= 1 with a bounded timeout, proving the loopback listener and Chromium path are capable of producing observable traffic when the suite executes.
- The forbidden credential-popup assertion is made after the browser action completes plus a bounded late-arrival window, reducing the chance of a timing-induced false PASS.
- The fixture remains hermetic: temporary profile, loopback TCP listener, synthetic credentials, no DNS/external site/user account.
- No executable .NET/Chromium PASS is claimed in this connector-only environment. No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing: remote-HTTP or credential-bearing HTTP(S) pages cannot be adopted, selected as fallback, marked permitted in a snapshot, or accepted as a download source by the session driver.
- The strengthened popup fixture now distinguishes pre-dispatch protection from merely closing/rejecting an unsafe page after its HTTP request reached the destination.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated persistence.
- Consequential actions require approval; prompt-injection gates, quarantine and audit boundaries remain intact.
- Emergency cancellation is pinned between steps and closes an agent page for an already in-flight side-effecting Playwright action. It cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium. Do not claim every redirect hop or popup request is blocked correctly until that evidence passes.
- The 350 ms forbidden-popup observation window is intentionally bounded to keep the integration suite deterministic; if Windows/CI scheduling proves unusually delayed, prefer an explicit network-idle/server-observation synchronization strategy rather than simply inflating sleeps indefinitely.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task; callers must not silently reuse the stopped page.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Execute the complete opt-in Chromium browser suite on the restored Windows/.NET 8 + Playwright Chromium environment and treat the server-side redirect, WebSocket and credential-popup counters as decisive network-boundary evidence. If any forbidden destination receives traffic, fix the production interception boundary rather than weakening the tests. Once the browser boundary passes, shift effort back to judge-visible end-to-end validation: run the live-demo readiness path with Nebius/Nemotron + Tavily and fix the highest-value real integration failure.
