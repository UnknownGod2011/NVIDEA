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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation now closes the agent-owned page before returning cancellation so a Playwright operation cannot silently continue behind a stopped executor. Canonical `BrowserPageAdmissionPolicy` is integrated into the session driver so initial-page validation, popup adoption, fallback selection, snapshots and download-source validation share transport + host admission semantics. Credential-popup integration evidence now uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup after traffic escaped.

### 2026-09-18 — browser risk-term precision (latest run)
Completed:
- Re-read this ledger, current browser executor/safety policy, existing browser-agent tests, recent commits, and current official Playwright routing guidance before mutation.
- Found an approval-fatigue bug in `BrowserSafetyPolicy`: consequential/sensitive terms were arbitrary substring matches, so harmless labels such as `Display settings`, `Design preview`, `Assignment details`, and `Secretary profile` collided with `pay`, `sign`, or `secret` and were incorrectly escalated.
- Replaced arbitrary substring matching with case-insensitive lexical term/phrase matching. Letters/digits are treated as word constituents; punctuation/whitespace/underscore boundaries delimit a term. This preserves conservative matches such as `Pay-now`, `SIGN`, `API key:`, and `Confirm order #42` while avoiding common false positives.
- Added regression theories pinning both sides of the boundary: harmless substring collisions remain normal medium-risk clicks without approval, while genuine bounded consequential/sensitive labels still require approval.
- Corrected the sensitive phrase fixture during review so it tests the exact configured phrase (`API key:`) rather than implying separator normalization that the matcher does not implement.
- Explicitly fetched repository metadata before every mutation and verified `repository_full_name` was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserAgentTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the matcher only accepts a configured term when both adjacent characters are absent or non-alphanumeric; embedded collisions therefore do not match while punctuation-delimited real terms do.
- Existing safety ordering remains unchanged: upload always requires approval; sensitive Type actions remain blocked; genuine sensitive/consequential targets remain high risk; prompt-injection-marked state-changing actions still require approval.
- Current official Playwright .NET guidance still recommends context routing for popup first requests and blocking Service Workers when relying on request interception; NVIDEA's existing context-level routing + Service Worker block remains aligned with that guidance.
- No executable .NET/Chromium PASS is claimed in this connector-only environment. No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing: remote-HTTP or credential-bearing HTTP(S) pages cannot be adopted, selected as fallback, marked permitted in a snapshot, or accepted as a download source by the session driver.
- Consequential actions require approval; sensitive autonomous typing remains blocked; prompt-injection gates, quarantine and audit boundaries remain intact.
- Risk keyword precision now reduces spurious approvals without weakening configured term matches, which is important because repeated false confirmations train users to approve reflexively.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated persistence.
- Emergency cancellation is pinned between steps and closes an agent page for an already in-flight side-effecting Playwright action. It cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium. Do not claim every redirect hop or popup request is blocked correctly until that evidence passes.
- The risk matcher deliberately does not perform stemming or separator normalization inside multi-word configured phrases; future additions should use explicit variants where UI wording materially differs rather than broadening back to substring matching.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task; callers must not silently reuse the stopped page.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Execute the complete opt-in Chromium browser suite on the restored Windows/.NET 8 + Playwright Chromium environment and treat the server-side redirect, WebSocket and credential-popup counters as decisive network-boundary evidence. If any forbidden destination receives traffic, fix the production interception boundary rather than weakening the tests. Once the browser boundary passes, shift effort back to judge-visible end-to-end validation: run the live-demo readiness path with Nebius/Nemotron + Tavily and fix the highest-value real integration failure.
