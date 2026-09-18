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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback, rejecting embedded URI credentials. Added context request/WebSocket routing, Service Worker blocking, post-action observed-location enforcement, download quarantine, credential typing/prompt-injection/consequential-action gates, persistent authenticated-state restart coverage, redirect/WebSocket server-side no-dispatch fixtures, and deterministic emergency-stop coverage. Production in-flight cancellation now closes the agent-owned page before returning cancellation so a Playwright operation cannot silently continue behind a stopped executor. Canonical `BrowserPageAdmissionPolicy` is integrated into the session driver so initial-page validation, popup adoption, fallback selection, snapshots and download-source validation share transport + host admission semantics. Credential-popup integration evidence uses server-side counters and a positive control to distinguish pre-dispatch blocking from merely closing an unsafe popup after traffic escaped. Risk keyword matching was changed from arbitrary substrings to lexical term/phrase boundaries to avoid approval fatigue from labels such as `Display settings`, `Design preview`, `Assignment details`, and `Secretary profile` while preserving genuine risk terms.

### 2026-09-18 — expanded credential/identity boundary (latest run)
Completed:
- Re-read this ledger, recent commits, production `BrowserSafetyPolicy`, and browser-agent regression tests before mutation.
- Audited the sensitive-field vocabulary and found material gaps: common authentication/recovery/token/identity labels such as `verification code`, `security code`, `PIN`, `seed phrase`, `recovery code`, `backup code`, `access token`, `refresh token`, and `SSN` were not classified as sensitive. A model could therefore type into those fields without the intended hard credential boundary if the page was otherwise trusted.
- Expanded `SensitiveFieldTerms` with those categories plus one-time-password, recovery-phrase, bearer-token, and Social Security Number variants. Existing lexical-boundary matching remains in force, so this does not restore broad substring matching.
- Updated the block reason to accurately include identity numbers and recovery material.
- Added deterministic theory coverage proving autonomous Type actions into the expanded sensitive labels are blocked rather than merely approval-gated, and proving non-Type interactions with representative sensitive controls require explicit approval.
- Explicitly fetched repository metadata before every mutation and verified `full_name` was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserAgentTests.cs`
- `progress.md`

Validation/evidence:
- Static review confirms the expanded terms flow through the same fail-closed branch as existing password/API-key detection: Type => blocked; other matching interactions => high risk + approval.
- Existing lexical matching still requires bounded configured terms, preserving the previous approval-fatigue fix.
- Tests are committed for 10 sensitive Type labels and four non-Type sensitive controls, but no executable .NET PASS is claimed in this connector-only environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama, or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Agent-visible page admission is defense-in-depth with request routing: remote-HTTP or credential-bearing HTTP(S) pages cannot be adopted, selected as fallback, marked permitted in a snapshot, or accepted as a download source by the session driver.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels; prompt-injection gates, quarantine and audit boundaries remain intact.
- Risk keyword precision reduces spurious approvals without weakening configured term matches, limiting approval fatigue.
- Service Workers remain disabled in the agent-owned context to reduce request-routing bypass while browser-managed cookies/local storage remain available for authenticated persistence.
- Emergency cancellation is pinned between steps and closes an agent page for an already in-flight side-effecting Playwright action. It cannot retroactively undo an external side effect committed before cancellation.

## Known blockers / risks
- Real-Chromium redirect, WebSocket, profile-persistence, popup pre-dispatch and in-flight cancellation fixtures still need execution on Windows with .NET 8, restored packages and Playwright Chromium. Do not claim every redirect hop or popup request is blocked correctly until that evidence passes.
- Sensitive-field classification is intentionally explicit rather than semantic; unfamiliar site-specific labels can still evade vocabulary matching. The stronger long-term design is to combine deterministic terms with DOM attributes/autocomplete semantics and page-element metadata without trusting model rationale alone.
- The risk matcher deliberately does not perform stemming or separator normalization inside multi-word configured phrases; future additions should use explicit variants where UI wording materially differs rather than broadening back to substring matching.
- Closing a page on emergency cancellation requires explicit fresh-page recovery before a later task; callers must not silently reuse the stopped page.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
Execute the complete opt-in Chromium browser suite on the restored Windows/.NET 8 + Playwright Chromium environment and treat the server-side redirect, WebSocket and credential-popup counters as decisive network-boundary evidence. If that environment remains unavailable, harden sensitive form classification using trusted DOM/accessibility metadata (especially HTML autocomplete/type semantics) so password/OTP/payment fields do not depend solely on visible-label vocabulary. Once the browser boundary passes, shift effort back to judge-visible end-to-end validation with Nebius/Nemotron + Tavily and fix the highest-value real integration failure.
