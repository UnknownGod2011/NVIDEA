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

### 2026-09-17 — local submission evidence hardening
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, process-level fault injection, and an independent receipt verifier. The independent verifier is mandatory before production PASS. Added static and behavioral receipt-tamper regressions without adding a production bypass/test hook. Added `scripts/test-submission-tooling.ps1` as the deterministic single entry point for all six zero-cost submission regressions, with optional repository-confined machine-readable evidence capture.

### 2026-09-17 — recording-day readiness hardening
Added `scripts/live-demo-readiness.ps1` and strengthened it to require Windows, PowerShell 7+, an installed .NET 8 SDK, solution presence, Nebius/Tavily configuration, optional cloud-research topology/credential presence, accessible configured PEM files without reading their contents, and opt-in `dotnet build --no-restore` validation. Added an always-on schema-v2 demo-package gate for the 180-second limit, unique beats, feature paths, and the exact six production-observed milestone sequence. Extended the gate to validate evidence metadata/files and verification-command projects. Hardened repository asset validation against both lexical traversal and symlink/junction resolved-target escape.

### 2026-09-17 — secure browser transport boundary
Blocked intentional remote plaintext HTTP navigation while preserving HTTPS and HTTP loopback development endpoints. Added focused transport-policy coverage. Added post-action observed-location enforcement so redirects to unsafe destinations cannot be verified or followed by further autonomous actions.

### 2026-09-17 — pre-network Playwright transport enforcement (latest run)
Completed:
- Re-read this ledger completely and inspected `PersistentBrowserContextFactory`, the existing transport policy, browser package version, and transport tests before changing code.
- Verified against current official Playwright .NET documentation that `BrowserContext.RouteAsync` can intercept context-wide requests and abort them, including popup traffic, while request interception does not cover Service Worker traffic unless Service Workers are blocked.
- Hardened the production persistent Chromium context with a context-wide `**/*` route before the first agent page/navigation. Every outgoing request is now evaluated by the same `BrowserSafetyPolicy.EvaluateObservedLocation` HTTPS-or-loopback rule; unsafe or unparsable destinations are aborted with `blockedbyclient` before network dispatch.
- Set `ServiceWorkers = ServiceWorkerPolicy.Block` on the persistent context because Playwright documents that routing does not intercept Service Worker requests. This closes a bypass around the transport boundary while retaining ordinary cookie/local-storage authenticated profile state.
- Tightened the initial `startUri` validation to the same policy, so a persistent authenticated context cannot be launched directly onto remote plaintext HTTP.
- Retained the executor's post-action observed-location check as independent defense in depth rather than replacing it.
- Extended transport API coverage to pin HTTPS + HTTP-loopback acceptance and remote HTTP/file rejection.
- Explicitly verified repository metadata as `full_name=UnknownGod2011/NVIDEA` before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/PersistentBrowserContextFactory.cs`
- `tests/Nvidea.Core.Tests/PersistentBrowserTransportApiSurfaceTests.cs`
- `progress.md`

Validation/evidence:
- Official Playwright .NET docs confirm context routing applies to requests from pages in the context and `Route.AbortAsync` aborts the request; docs also explicitly recommend blocking Service Workers when request interception is relied upon.
- Repository source review confirms the route is registered before stale pages are discarded, before the fresh page is created, and before the first `GotoAsync`, so the initial navigation and later redirects/popups are inside the transport boundary.
- `Microsoft.Playwright` is pinned at 1.62.0 in `Nvidea.Core.csproj`; no dependency change was required.
- No executable .NET test PASS is claimed in this connector environment. The new route behavior still needs restored Chromium/.NET integration execution.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright browser session, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Requested navigation, actual post-action destinations, and now pre-network Playwright requests share one HTTPS-or-loopback transport invariant.
- Remote plaintext redirects are aborted before browser cookies/form data/authenticated state can be sent, rather than merely detected after navigation.
- Service Workers are disabled in the agent-owned persistent context specifically to prevent them bypassing Playwright request routing; normal cookie/local-storage authentication persistence remains available.
- Unsafe/unparsable request URLs fail closed. The post-action check remains a separate layer if browser state and route observations ever diverge.
- Existing blocks on autonomous credential typing and approval gates for consequential or prompt-injection-exposed mutations remain intact.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.
- Manifest-controlled feature, evidence, and command-project paths remain protected against both lexical and resolved-target escape.

## Known blockers / risks
- The context-route redirect behavior and complete browser suite still need executable confirmation on restored .NET 8 + Playwright Chromium, especially a local HTTPS → remote-HTTP redirect integration fixture proving the remote request never reaches its server.
- Blocking Service Workers can affect sites whose authenticated/product flows depend materially on workers; judge-path sites must be tested and any incompatibility handled without weakening the transport rule.
- The strengthened live readiness preflight and unified submission suite still need execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Current connector execution cannot establish live provider or authenticated browser success; those claims must remain unmade until matching production evidence exists.

## Single Best Next Task
Add a real Playwright Chromium integration test with local loopback fixtures that proves a permitted page attempting to redirect/navigate to a remote-plaintext URL is aborted before that destination receives a request, while HTTPS/loopback navigation and authenticated profile persistence still work. Then run the restored browser tests and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` on the actual Windows recording machine before live judge-path capture.
