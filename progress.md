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

### 2026-09-18 — executable browser network-boundary fixtures
Added opt-in real-Chromium fixtures enabled by `NVIDEA_RUN_PLAYWRIGHT_INTEGRATION=1`. They exercise production `PersistentBrowserContextFactory` against controlled loopback servers. Redirect and WebSocket checks require credential-bearing forbidden destinations to produce zero destination-side requests/handshakes, with positive controls and bounded late-arrival observation to reduce false passes.

### 2026-09-18 — persistent authenticated-state restart coverage (latest run)
Completed:
- Re-read this ledger and the existing real-Chromium fixture before mutation.
- Added `PersistentProfile_RestartPreservesCookieAndLocalStorage_WithoutRestoringStaleTabs` to the production-factory integration suite.
- The test launches the actual persistent context against a synthetic loopback origin, writes a synthetic SameSite cookie plus local-storage preference, deliberately opens a second stale tab, cleanly closes the context, then relaunches using the identical user-data directory.
- After restart it requires exactly one fresh page, verifies the cookie and local-storage value survived, and verifies no stale-tab URL was restored. This pins the intended authenticated-profile contract without using a real account or secret.
- Kept the fixture hermetic: loopback only, temporary profile directory, synthetic values, no provider/account/network dependency beyond local sockets.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `tests/Nvidea.Core.Tests/PersistentBrowserRedirectIntegrationTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the new persistence check exercises production `PersistentBrowserContextFactory` twice with the same state directory rather than mocking persistence.
- The assertion simultaneously covers positive state persistence and negative stale-tab restoration, so retaining authentication cannot silently expand the browser's resumed navigation surface.
- No executable Chromium PASS is claimed here. The integration suite remains opt-in and requires restored .NET 8 dependencies plus installed Playwright Chromium.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated user browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Page navigation/request transport permits HTTPS or HTTP loopback only; WebSocket transport permits WSS or WS loopback only; all four forms reject embedded URI credentials.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated profile persistence.
- Unsafe/unparsable request and socket URLs fail closed. Existing credential-typing blocks, consequential-action approvals, prompt-injection gates, download quarantine and emergency-stop architecture remain intact.
- Redirect and WebSocket validation are hermetic and use loopback sockets plus synthetic non-secret credential markers. Network-boundary counters are used instead of browser-only success signals.
- Profile-persistence validation uses only synthetic cookie/local-storage values and a disposable temporary profile; it does not copy or inspect a user's real browser profile.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.

## Known blockers / risks
- The real-Chromium redirect, WebSocket and profile-persistence fixtures still need execution on a machine with .NET 8, restored packages and Playwright Chromium. Until the redirect test passes, NVIDEA must not claim every redirect hop is blocked before dispatch.
- If `/forbidden-destination` is observed, BrowserContext request routing is insufficient for redirect-hop enforcement and must be replaced/hardened before relying on it for authenticated flows.
- If `/forbidden-websocket` is observed, the WebSocket routing boundary is not enforcing its intended no-handshake property and must be corrected before authenticated judge flows.
- If the profile restart test fails, persistent authenticated browser sessions are not recording-ready; determine whether Chromium shutdown semantics, factory startup behavior, or storage persistence is responsible before weakening assertions.
- Blocking Service Workers can affect sites whose product/auth flows materially depend on workers; judge-path sites need compatibility testing without weakening the transport rule.
- The strengthened live readiness preflight and unified submission suite still need execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Run the opt-in Chromium redirect + WebSocket + profile-persistence fixtures on the restored Windows/.NET 8 environment with Playwright Chromium installed. Fix any observed production behavior rather than weakening the network/security assertions. If all three boundaries pass, add a hermetic cancellation/emergency-stop integration scenario proving a multi-step browser plan cannot execute a queued consequential action after cancellation, then run the full browser suite and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` before live judge-path capture.
