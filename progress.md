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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation now closes the agent-owned page before returning cancellation so a Playwright operation cannot silently continue behind a stopped executor.

### 2026-09-18 — canonical agent-visible page admission boundary (latest run)
Completed:
- Re-read this ledger, recent commits, `PersistentBrowserContextFactory`, `PlaywrightBrowserDriver`, and `PlaywrightBrowserSessionDriver` before mutation.
- Found a defense-in-depth mismatch: `PlaywrightBrowserSessionDriver.IsPermitted` currently checks only HTTP(S) plus optional host allowlist. It does not itself enforce the canonical transport requirements for HTTPS-or-loopback or embedded URI credentials. Context routing normally blocks unsafe network dispatch, but page/tab admission must not depend on that lower layer being perfect.
- Added `BrowserPageAdmissionPolicy`, which composes `BrowserSafetyPolicy.EvaluateObservedLocation` with the optional per-task host allowlist. This gives popup/tab adoption and active-page recovery a single fail-closed predicate matching the transport boundary.
- Added deterministic tests covering HTTPS, loopback HTTP (localhost/IPv4/IPv6), remote plaintext HTTP, credential-bearing HTTPS and loopback HTTP, file/about/javascript/non-URL inputs, case-insensitive host allowlisting, and composition of host + transport policy.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserPageAdmissionPolicy.cs` (new)
- `tests/Nvidea.Core.Tests/BrowserPageAdmissionPolicyTests.cs` (new)
- `progress.md`

Validation/evidence:
- The new admission predicate delegates transport semantics to the already-central `BrowserSafetyPolicy` rather than reimplementing HTTPS/loopback/user-info rules.
- Regression inputs pin both transport and host-boundary behavior without browser/network/provider dependencies.
- No executable .NET PASS is claimed in this connector-only environment. No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated persistence.
- Consequential actions require approval; prompt-injection gates, quarantine and audit boundaries remain intact.
- Emergency cancellation is pinned between steps and closes an agent page for an already in-flight side-effecting Playwright action. It cannot retroactively undo an external side effect committed before cancellation.
- The new page-admission predicate is intentionally separate until wired into the session driver; until then the existing session driver's weaker local `IsPermitted` remains a known defense-in-depth gap even though context routing is still active.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium. Do not claim every redirect hop is blocked pre-dispatch until that evidence passes.
- `BrowserPageAdmissionPolicy` must now replace the duplicated `IsPermitted` logic inside `PlaywrightBrowserSessionDriver`; do not leave two divergent admission definitions.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task; callers must not silently reuse the stopped page.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Wire `BrowserPageAdmissionPolicy` into `PlaywrightBrowserSessionDriver` for initial-page validation, popup/tab adoption, fallback selection, snapshots and download-source validation, deleting the duplicated weaker predicate. Add a regression proving a credential-bearing or remote-HTTP popup can never become active even when the host allowlist would otherwise admit it. Then execute the full deterministic browser suite plus opt-in Chromium fixtures on the restored Windows/.NET 8 environment; fix production behavior rather than weakening assertions.
