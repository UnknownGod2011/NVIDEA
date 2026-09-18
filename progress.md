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

### 2026-09-18 — canonical page admission integrated into session driver (latest run)
Completed:
- Re-read this ledger, recent commits, `BrowserPageAdmissionPolicy`, and the complete production `PlaywrightBrowserSessionDriver` before mutation.
- Removed the session driver's weaker duplicated admission semantics by constructing one `BrowserPageAdmissionPolicy` from `PlaywrightBrowserDriverOptions.NormalizedAllowedHosts` and routing all local admission decisions through it.
- Initial-page validation, popup/new-tab adoption, active-page fallback, session snapshot `IsPermitted`, and download-source validation now all require BOTH the canonical transport rule (HTTPS or loopback HTTP, no embedded URI credentials) and the optional per-task host allowlist.
- Unsafe HTTP(S) popup candidates are still closed without becoming active; transient non-web pages such as a newly-created `about:blank` remain pending for bounded classification rather than being mistaken for a permitted page.
- Updated boundary/error wording to make the combined transport + host contract explicit.
- Explicitly fetched repository metadata immediately before each mutation and verified `full_name` was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/PlaywrightBrowserSessionDriver.cs`
- `progress.md`

Validation/evidence:
- The production session driver now delegates its `IsPermitted` decision directly to `BrowserPageAdmissionPolicy`; there is no second host-only implementation in that class.
- Existing deterministic `BrowserPageAdmissionPolicyTests` already pin HTTPS, localhost/IPv4/IPv6 loopback, remote plaintext HTTP, credential-bearing URLs, non-web schemes, case-insensitive host allowlisting and transport+host composition.
- Code review confirms the same predicate is reached by constructor validation, candidate adoption, fallback selection, snapshots and download capture.
- No executable .NET PASS is claimed in this connector-only environment. No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is now defense-in-depth with request routing: even if a lower network boundary regresses, a remote-HTTP or credential-bearing HTTP(S) page cannot be adopted, selected as fallback, marked permitted in a snapshot, or accepted as a download source by the session driver.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated persistence.
- Consequential actions require approval; prompt-injection gates, quarantine and audit boundaries remain intact.
- Emergency cancellation is pinned between steps and closes an agent page for an already in-flight side-effecting Playwright action. It cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium. Do not claim every redirect hop is blocked pre-dispatch until that evidence passes.
- A dedicated real-browser popup regression should prove a credential-bearing or remote-HTTP popup cannot become active even when its host is allowlisted; deterministic policy coverage exists, but browser event timing should also be exercised.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task; callers must not silently reuse the stopped page.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Add a hermetic real-Chromium popup-admission fixture using controlled loopback pages: allow the source host, open a safe popup as positive control, then attempt a credential-bearing popup and prove it is never adopted as the active agent page and is closed after classification. If practical, add a remote-HTTP classification case without sending traffic to an external host. Then run the full deterministic browser suite plus opt-in Chromium fixtures on the restored Windows/.NET 8 environment and fix production behavior rather than weakening assertions.
