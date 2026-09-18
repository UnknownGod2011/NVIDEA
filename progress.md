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

### 2026-09-18 — hermetic credential-popup browser fixture (latest run)
Completed:
- Re-read this ledger, recent commits, the production session-driver integration and existing real-Chromium persistent-session fixture before mutation.
- Strengthened `PersistentBrowserSessionIntegrationTests.AllowedPopup_BecomesActive_AndCredentialBearingPopup_IsNeverAdopted` so the negative case is now fully hermetic and directly exercises the canonical no-URI-credentials rule.
- Positive control still proves a normal same-boundary loopback popup becomes the active agent page.
- Negative control now opens `http://synthetic-user:synthetic-password@127.0.0.1:<fixture-port>/credential-popup`: the host is intentionally the SAME allowlisted loopback host, so host allowlisting alone cannot make the test pass. The popup must be rejected because its authority contains user-info.
- Removed the previous `example.invalid` dependency; the popup classification case no longer needs external DNS/network behavior.
- Snapshot assertions require every retained page to be permitted, contain no `UserInfo`, remain on the allowlisted host, and exclude `/credential-popup`.
- The local test server includes a `/credential-popup` body only as a sentinel if Chromium reaches it; no real credentials/accounts are involved.
- Explicitly fetched repository metadata immediately before both mutations and verified `full_name` was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `tests/Nvidea.Core.Tests/PersistentBrowserSessionIntegrationTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the positive and negative popup paths share the same production `BrowserHostRuntime`/session driver and the negative host is deliberately allowlisted, isolating credential-bearing URI admission as the property under test.
- The fixture is hermetic: temporary browser profile, loopback TCP server, synthetic username/password, no external site, no user account.
- Existing deterministic `BrowserPageAdmissionPolicyTests` separately pin credential-bearing HTTP(S) rejection; this integration test adds real browser event/adoption timing coverage when enabled.
- No executable .NET/Chromium PASS is claimed in this connector-only environment. No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing: remote-HTTP or credential-bearing HTTP(S) pages cannot be adopted, selected as fallback, marked permitted in a snapshot, or accepted as a download source by the session driver.
- The new popup fixture specifically prevents a future regression where an allowlisted host accidentally overrides the no-user-info transport rule during popup adoption.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated persistence.
- Consequential actions require approval; prompt-injection gates, quarantine and audit boundaries remain intact.
- Emergency cancellation is pinned between steps and closes an agent page for an already in-flight side-effecting Playwright action. It cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence, popup-admission and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium. Do not claim every redirect hop or popup event is blocked correctly until that evidence passes.
- The credential-popup fixture proves non-adoption/retention; a future server-side request counter can additionally prove whether the credential-bearing popup request is prevented before network dispatch rather than merely closed after classification.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task; callers must not silently reuse the stopped page.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Extend the hermetic credential-popup fixture with a server-side request counter for `/credential-popup` and assert zero destination requests after a bounded observation window. This distinguishes strong pre-dispatch blocking from post-navigation page rejection. Then execute the complete opt-in Chromium browser suite on the restored Windows/.NET 8 environment and fix production behavior if any network-boundary assertion fails rather than weakening the tests.
