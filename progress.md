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

### 2026-09-17 to 2026-09-18 — secure browser transport boundary
Blocked intentional remote plaintext HTTP navigation while preserving HTTPS and HTTP loopback development endpoints. Added post-action observed-location enforcement so unsafe destinations cannot be verified or followed by autonomous actions. Added context-wide Playwright request routing before fresh-page creation, with Service Workers blocked so ordinary HTTP(S) requests are policy-routed. Added separate context-wide WebSocket routing: WSS and loopback WS are allowed; remote plaintext WS is closed without connecting to the real server. Existing credential typing blocks, consequential approvals, prompt-injection gates, download quarantine and emergency-stop architecture remain intact.

### 2026-09-18 — credential-bearing transport hardening (latest run)
Completed:
- Re-read this ledger completely and inspected `BrowserSafetyPolicy`, `PersistentBrowserContextFactory`, and current transport tests before changing code.
- Fresh official Playwright .NET docs were reviewed for BrowserContext/Page routing, WebSocket routing, Service Worker limitations, and route abort behavior.
- Identified a distinct credential-leak gap: HTTPS/WSS and loopback HTTP/WS destinations containing URI user-info (`user:password@host` or `token@host`) previously passed the transport policy. Such credentials can leak through agent plans, URL/history surfaces, redirects, diagnostics, or audit evidence and bypass the product's explicit credential/session boundaries.
- Hardened both web and WebSocket transport predicates to fail closed whenever `Uri.UserInfo` is non-empty, regardless of otherwise-safe scheme or loopback status. Credentials must instead remain in explicit browser/session mechanisms.
- Updated user-facing policy reasons so credential-bearing destinations are explicitly classified as blocked.
- Added regression coverage for username/password and token-style user-info on HTTPS/WSS plus loopback HTTP/WS, while retaining positive HTTPS/WSS and loopback cases.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `tests/Nvidea.Core.Tests/PersistentBrowserTransportApiSurfaceTests.cs`
- `progress.md`

Validation/evidence:
- Static source review confirms the shared URI safety predicates are used by explicit navigation, observed-location enforcement, HTTP(S) request routing, start-URI validation, and WebSocket routing, so the user-info rejection applies consistently across those policy entry points.
- Official Playwright .NET documentation confirms BrowserContext routing covers requests from pages in the context, Service Workers can evade ordinary routing unless blocked, WebSocket routing is a separate API, and aborted routes do not proceed normally.
- Important documentation nuance retained as an open executable-validation item: Playwright's Page routing docs state the handler is called only for the first URL when a response redirects. BrowserContext docs do not make the same redirect guarantee explicit. Therefore this ledger does NOT claim that every redirect hop is pre-dispatch blocked until the server-observation integration fixture proves the actual BrowserContext behavior used here.
- No executable .NET/Chromium PASS is claimed in this connector environment. The new policy tests require restored .NET 8 execution.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser session, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Page navigation/request transport permits HTTPS or HTTP loopback only; WebSocket transport permits WSS or WS loopback only; all four transport forms now reject embedded URI credentials.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated profile persistence.
- Unsafe/unparsable request and socket URLs fail closed. Existing credential-typing blocks, consequential-action approvals, prompt-injection gates, download quarantine and emergency-stop architecture remain intact.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.

## Known blockers / risks
- Browser transport hardening still needs executable confirmation on restored .NET 8 + Playwright Chromium. The highest-priority uncertainty is redirect behavior: prove at the destination server that a permitted page cannot cause a non-loopback plaintext HTTP redirect request, rather than relying on routing assumptions.
- The same integration fixture should prove remote `ws://` refusal at server-observation level and exercise credential-bearing URL rejection without allowing secrets into test logs.
- Blocking Service Workers can affect sites whose product/auth flows materially depend on workers; judge-path sites need real compatibility testing without weakening the transport rule.
- The strengthened live readiness preflight and unified submission suite still need execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Implement and execute a real Playwright Chromium integration fixture with controlled loopback servers and server-side connection counters. First settle BrowserContext redirect semantics empirically: prove whether an allowed initial request that returns a redirect toward a policy-forbidden destination is stopped before that destination receives a request; if not, replace the routing strategy with a mechanism that can enforce every redirect hop before network dispatch. In the same fixture prove remote plaintext WebSocket refusal, credential-bearing destination rejection, allowed loopback transport, and authenticated profile persistence. Then run restored browser tests and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` on the Windows recording machine before live judge-path capture.
