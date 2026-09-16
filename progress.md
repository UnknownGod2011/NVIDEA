# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Dispatch-binding V2 signs authoritative remote id + canonical SHA-256 of the exact encrypted work-item envelope; worker verifies and pins the envelope before execution.
- Protected local CAS state supports durable binding-publication, audit and protected-payload-cleanup obligations with independent restart recovery.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-12
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment preflight and judging/evaluator tooling.

### 2026-09-13 to 2026-09-15
Hardened exact-once browser behavior and remote dispatch: durable external-action/cleanup/audit intents, exact remote provenance, crash-resumable cancellation, envelope commitment, V2 sender authenticity, pinned-envelope worker execution, bounded worker transport retry/SIGTERM, atomic reservation + audit + digest CAS, provider-delivery ambiguity reconciliation, shared reservation trust validation and final pre-Create durable authority revalidation.

### 2026-09-15 to 2026-09-16
Added durable V2 binding obligations, restart reconciliation, result/audit/cleanup crash recovery, ambiguous-delete handling, bounded cleanup CAS convergence, multi-artifact cleanup races, and a composed production-path regression covering V2 publication + result CAS/audit + encrypted deletion + concurrent terminal progress + final binding reconciliation without replay.

### 2026-09-16 — judge-visible runtime evidence
Added `JudgeEvidenceDialog` backed by real `DesktopResearchReadiness`, plus `SessionEvidenceLedger`, a closed process-local kind+timestamp proof boundary. Desktop chat/research record successful Nemotron inference, actual memory influence, and Tavily research only when a validated citation was used. Added fail-closed browser evidence for completed jobs with trusted verified-step checkpoints; consequential approval proof is recorded only after the exact-scope trusted-host boundary returns.

### 2026-09-16 — browser evidence composition
- Shared `SessionEvidenceLedger.ProcessLocal` across desktop and browser product runtime.
- Added `EvidenceObservingBrowserGoalHost` for normal multi-step goal execution.
- Hardened regressions so pending/running/waiting/retry/failed/cancelled/unverified browser outcomes never become proof.
- Routed `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` through the evidence decorator.
- Reused one `JsonBrowserGoalSessionStore` per composition-root lifetime across goal agent, listing, and ambiguous recovery.
- Added a composition regression proving the internal factory returns the evidence decorator rather than raw browser authority without starting Playwright.

### 2026-09-16 — trusted Nebius background evidence
- Added `EvidenceObservingRemoteResearchClientRuntime`, an observation-only decorator over `IRemoteResearchClientRuntime`, composed immediately after the real Nebius runtime and before `ResearchCloudExecutionCoordinator`.
- Dispatch and cancellation requests remain evidence-free; reconciliation/recovery can record `NebiusBackgroundExecutionObserved` only after authenticated result ingestion returns durable `ResultApplied` provenance locally.
- Hardened the boundary so `ResultApplied + Local` is insufficient while a durable `PendingAuditEvent` remains; judge proof is downstream of both authenticated result CAS and its accountability append.
- Added an internal composition seam for isolated testing without provider/network activity.

### 2026-09-16 — remote evidence adversarial contract suite
- Added isolated adversarial coverage proving dispatch/cancel, non-result lifecycle states, pending-audit crash windows and thrown reconciliation remain non-evidence.
- Successful audited `ResultApplied` recovery establishes `NebiusBackgroundExecutionObserved` exactly once with first-observation semantics.

### 2026-09-16 — safe new demo session UX
- Added `DesktopInvocationService.ResetSessionEvidence()` as a deliberately narrow boundary that can reach only the injected ephemeral session ledger.
- Added a `New demo session` control to `JudgeEvidenceDialog`, with explicit Yes/No confirmation defaulting to No and precise disclosure that durable memory/jobs/browser/audit state is preserved.
- Changed dialog composition from a frozen snapshot to narrow snapshot/reset delegates so the UI refreshes immediately without receiving durable-store authority.

### 2026-09-16 — desktop evidence reset contract
- Added a regression that establishes genuine Nemotron completion evidence through `DesktopInvocationService.InvokeAsync`, clears the exact injected ledger through `ResetSessionEvidence`, and proves a later genuine invocation can establish fresh evidence.
- The test checks inference counts so reset itself cannot invoke the model or manufacture proof.

### 2026-09-16 — deterministic judge demo runbook (latest run)
Completed:
- Re-read this ledger completely and inspected the existing machine-checkable `docs/demo-package.json` plus `docs/demo-package.md` before mutation.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation; no other repository was mutated.
- Added `docs/judge-demo-runbook.md`, an operator-facing <=3-minute recording plan aligned to the existing 168-second manifest.
- Mapped every required judging beat to a concrete UI/product action, expected visible result, production session-evidence milestone where applicable, preflight dependency, and fail-closed fallback.
- Added a strict preflight gate covering Windows builds/evals, global invocation/emergency stop, Nemotron readiness, live Tavily, authenticated browser state, consequential approval, optional live Nebius PASS evidence, and a clean New demo session evidence projection.
- Added a post-take rejection gate for missing milestones, ungrounded citations, invisible browser verification, ungated consequential actions, overclaimed provider-live evidence, exposed secrets/private payloads, bypassed login/CAPTCHA/MFA safeguards, or >180-second recordings.
- Preserved the evidence-class boundary: synthetic evaluator evidence is supporting engineering evidence and cannot be narrated as live Tavily/Nebius provider proof.

Files changed this run:
- `docs/judge-demo-runbook.md`
- `progress.md`

Validation/evidence:
- Static audit confirms the runbook timing exactly follows the existing manifest: 20 + 22 + 28 + 28 + 20 + 28 + 22 = 168 seconds, leaving 12 seconds contingency under 180 seconds.
- Each required product beat now names the relevant production proof boundary: Nemotron completion, memory influence, validated Tavily citation, trusted browser post-state verification, exact-scope consequential approval, and audited authenticated Nebius result application.
- The runbook explicitly fails closed instead of suggesting terminal-only substitution for Windows UX, manual completion for browser approval, or synthetic evidence substitution for live providers.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local, payload-free and intentionally non-durable; reset is a demo-evidence operation, not an audit or privacy deletion feature.
- The demo runbook prohibits displaying secrets, cookies, tokens, raw checkpoints, private browser/session data, or provider exceptions.
- Browser fallback explicitly forbids bypassing login, CAPTCHA, MFA or site safeguards.
- Consequential actions must remain behind the trusted exact-scope approval boundary; a missing approval dialog invalidates the take.
- Provider evidence remains typed: synthetic/local-live/provider-live/documentation are not interchangeable claims.
- A missing expected session milestone invalidates the corresponding demo claim instead of being papered over in narration.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Product-level and ambiguous-recovery browser evidence observation remain separate; ledger idempotence prevents proof inflation, but redundant observation should be removed only after equivalent host-level coverage is proven.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- The WPF reset flow and new regression should be executed on Windows/.NET 8 before submission; any compile/XAML/test issue must be fixed rather than bypassing confirmation.
- The final recording still requires a real Windows demo-machine preflight. In particular, live Tavily and any claimed provider-live Nebius background execution cannot be validated from this environment.

## Single Best Next Task
Turn the new runbook into a stronger machine-checkable contract by extending the demo-package schema/validator with explicit per-beat expected session milestone, preflight dependency and fallback-policy fields, then add fail-closed validator regressions so documentation and the executable judging manifest cannot silently drift apart.