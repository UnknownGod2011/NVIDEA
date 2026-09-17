# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-16
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, extensive crash-consistency hardening, judge-visible runtime evidence, a 168-second deterministic demo, schema-v2 per-beat execution contracts, hardened validator/regressions, and manifest/runbook alignment.

### 2026-09-17 — submission / recording readiness
Added manifest-driven checklist generation, adversarial submission validation, independent receipts, deterministic zero-cost regressions, machine-readable evidence, and `scripts/live-demo-readiness.ps1`. Recording readiness requires Windows, PowerShell 7+, .NET 8, solution/configuration presence, optional cloud topology/PEM presence, repository-confined demo/evidence/project paths, schema-v2 demo sequencing and optional no-restore build validation. Repository asset validation resists lexical traversal and symlink/junction target escape.

### 2026-09-17 — secure browser transport boundary
Blocked intentional remote plaintext HTTP navigation while preserving HTTPS and HTTP loopback development endpoints. Added post-action observed-location enforcement so unsafe redirects cannot be verified or followed by autonomous actions. Added context-wide pre-network Playwright routing, registered before fresh-page creation, so HTTP(S) redirects/popups are checked before dispatch. Service Workers are blocked because Playwright documents that ordinary request routing cannot intercept their traffic.

### 2026-09-18 — WebSocket transport boundary (latest run)
Completed:
- Re-read this ledger completely and inspected `BrowserSafetyPolicy`, `PersistentBrowserContextFactory`, current browser transport tests and Microsoft.Playwright 1.62 usage before changing code.
- Fresh official Playwright .NET docs confirm `BrowserContext.RouteAsync` governs HTTP(S) requests but WebSockets have a separate `RouteWebSocketAsync` API; routed WebSockets do not connect to the real server unless `ConnectToServer()` is called. Playwright recommends registering WebSocket routing before pages create sockets.
- Identified a concrete gap: the existing HTTPS request route did not itself govern WebSocket handshakes. An authenticated page could therefore attempt a remote plaintext `ws://` connection outside NVIDEA's explicit transport policy.
- Added `BrowserSafetyPolicy.EvaluateWebSocketTransport`: `wss://` is allowed, `ws://` is allowed only for loopback development endpoints, and remote plaintext/other schemes fail closed.
- Added context-wide `RouteWebSocketAsync("**/*", ...)` before fresh agent-page creation. Allowed sockets call `ConnectToServer()`; blocked/malformed sockets are closed locally with WebSocket policy code 1008 and never call `ConnectToServer()`, so the real remote socket is not opened through Playwright's routed path.
- Retained HTTP(S) pre-network routing, Service Worker blocking, and post-action page-location checks as independent layers.
- Added policy regression coverage for WSS, localhost/IPv4/IPv6 loopback WS, remote WS rejection, and non-WebSocket scheme rejection.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `src/Nvidea.Core/Browser/PersistentBrowserContextFactory.cs`
- `tests/Nvidea.Core.Tests/PersistentBrowserTransportApiSurfaceTests.cs`
- `progress.md`

Validation/evidence:
- Official Playwright .NET BrowserContext documentation (current as of 2026-09-18) documents `RouteWebSocketAsync` as context-wide WebSocket interception and recommends calling it before pages create sockets.
- Official WebSocketRoute documentation states routed sockets do not connect to the server by default; `ConnectToServer()` is the explicit operation that establishes the actual server connection. The implementation only invokes it after the NVIDEA transport policy passes.
- Microsoft.Playwright 1.62.0 is already above the documented 1.48 introduction of WebSocket routing; no dependency change was required.
- Static source review confirms WebSocket routing is registered before stale pages are discarded and before the new agent page is created.
- No executable .NET/Chromium PASS is claimed in this connector environment. The new API usage and runtime behavior still require restored .NET 8 + Playwright Chromium execution.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, browser session, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Page navigation/request transport now permits HTTPS or HTTP loopback only; WebSocket transport permits WSS or WS loopback only.
- Remote plaintext HTTP requests are aborted before dispatch; remote plaintext WebSockets are routed and closed without connecting to the real server.
- Service Workers remain disabled in the agent-owned context to prevent request-routing bypass while browser-managed cookies/local storage remain available for authenticated profile persistence.
- Unsafe/unparsable request and socket URLs fail closed. Existing credential-typing blocks, consequential-action approvals, prompt-injection gates, download quarantine and emergency-stop architecture remain intact.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.

## Known blockers / risks
- Browser transport hardening still needs executable confirmation on restored .NET 8 + Playwright Chromium. In particular, integration coverage should prove both remote HTTP redirect abort and remote `ws://` refusal at the server-observation level.
- Blocking Service Workers can affect sites whose product/auth flows materially depend on workers; judge-path sites need real compatibility testing without weakening the transport rule.
- The strengthened live readiness preflight and unified submission suite still need execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add and execute a real Playwright Chromium loopback integration fixture that observes server-side connection counts: prove a permitted page cannot cause a request/redirect to a non-loopback plaintext HTTP endpoint or establish a non-loopback plaintext WebSocket, while HTTPS/WSS and loopback development transport plus authenticated profile persistence continue to work. Then run the restored browser tests and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` on the actual Windows recording machine before live judge-path capture.
