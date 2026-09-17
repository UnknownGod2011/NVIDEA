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

### 2026-09-18 — credential-bearing transport hardening
Hardened HTTP(S) and WebSocket transport predicates to reject any URI with non-empty `UserInfo`, including otherwise-safe HTTPS/WSS and loopback HTTP/WS. Added regression coverage so credentials must remain in explicit authenticated browser/session mechanisms instead of URL authority fields.

### 2026-09-18 — executable redirect-boundary fixture (latest run)
Completed:
- Re-read this ledger completely and inspected `PersistentBrowserContextFactory`, the Playwright driver options, test project, and recent commits before changing code.
- Rechecked current official Playwright .NET docs. `Page.RouteAsync` explicitly documents first-URL-only handling for redirects; `BrowserContext.RouteAsync` documents context-wide routing and Service Worker limitations but does not explicitly promise every redirect hop. `Route.ContinueAsync` also documents that it immediately sends the routed request and that URL/method/post-data overrides do not carry to redirects. The previous uncertainty therefore remains valid and needs executable evidence rather than assumption.
- Added `tests/Nvidea.Core.Tests/PersistentBrowserRedirectIntegrationTests.cs`, an opt-in real-Chromium fixture enabled by `NVIDEA_RUN_PLAYWRIGHT_INTEGRATION=1` after Playwright Chromium is installed.
- The fixture runs a controlled TCP HTTP server on loopback and records destination-side request counts. Its critical test serves an allowed loopback redirect whose `Location` contains URI user-info. Because credential-bearing destinations are forbidden even on loopback, the test can prove redirect interception without contacting any external network: the redirect source must be reached exactly once while `/forbidden-destination` must receive zero additional requests.
- Added a positive control proving ordinary loopback navigation reaches its destination, preventing a false security PASS caused by a completely broken browser/network fixture.
- The fixture uses a fresh NVIDEA browser state directory and closes the persistent context before best-effort cleanup. It does not print or persist real credentials; the only user-info token is a fixed synthetic test marker.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `tests/Nvidea.Core.Tests/PersistentBrowserRedirectIntegrationTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the fixture exercises the production `PersistentBrowserContextFactory` rather than a duplicate routing implementation, so it tests the same context-wide route and Service Worker configuration used by the product.
- The destination assertion is server-observed, not inferred from `Page.Url`, exceptions, or Playwright events; this is specifically designed to settle whether the forbidden redirect causes a real network request.
- A positive loopback control is included so destination non-observation is meaningful.
- No executable Chromium PASS is claimed in this connector environment. The integration assertions intentionally require `NVIDEA_RUN_PLAYWRIGHT_INTEGRATION=1`, restored .NET 8 dependencies, and installed Playwright Chromium.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated user browser session, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Page navigation/request transport permits HTTPS or HTTP loopback only; WebSocket transport permits WSS or WS loopback only; all four forms reject embedded URI credentials.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated profile persistence.
- Unsafe/unparsable request and socket URLs fail closed. Existing credential-typing blocks, consequential-action approvals, prompt-injection gates, download quarantine and emergency-stop architecture remain intact.
- The new redirect test is hermetic and uses only loopback sockets plus a synthetic non-secret credential marker. It deliberately avoids depending on public websites or external DNS.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.

## Known blockers / risks
- The new real-Chromium redirect fixture still needs execution on a machine with .NET 8, restored packages and Playwright Chromium. Until it passes, NVIDEA must not claim every redirect hop is blocked before dispatch.
- If `/forbidden-destination` is observed, the current BrowserContext routing strategy is insufficient for redirect-hop enforcement and must be replaced/hardened (likely with a Chromium-level interception boundary) before relying on it for credential-bearing authenticated flows.
- The fixture should next be extended with server-observed remote/plaintext WebSocket refusal and authenticated profile persistence.
- Blocking Service Workers can affect sites whose product/auth flows materially depend on workers; judge-path sites need real compatibility testing without weakening the transport rule.
- The strengthened live readiness preflight and unified submission suite still need execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Run the new opt-in Chromium fixture on the restored Windows/.NET 8 environment with Playwright Chromium installed. If the credential-bearing redirect destination receives zero requests, retain BrowserContext routing and add server-observed WebSocket plus authenticated-profile persistence coverage. If the destination receives any request, immediately replace/harden the routing mechanism with Chromium-level interception that demonstrably gates each redirect hop before dispatch. Then run the full browser suite and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` before live judge-path capture.
