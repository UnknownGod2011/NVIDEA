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

### 2026-09-16 — composition contract regression (latest run)
Completed:
- Re-read `progress.md` completely and inspected current composition root, evidence decorator, core test project, and repository tree before mutation.
- Explicitly verified immediately before each GitHub mutation that repository metadata reported `repository_full_name` exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `tests/Nvidea.Core.Tests/NvideaCompositionRootEvidenceCompositionTests.cs`.
- The regression calls the internal `NvideaCompositionRoot.CreateObservedBrowserGoalHost` seam with a passive `ICrashConsistentBrowserGoalHost` and asserts the returned object is the evidence decorator rather than the raw authority, without starting Playwright.
- The passive host throws for every browser operation, proving the factory test exercises composition only and cannot accidentally execute browser actions.

Files changed this run:
- `tests/Nvidea.Core.Tests/NvideaCompositionRootEvidenceCompositionTests.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms `Nvidea.Core.csproj` grants `InternalsVisibleTo` to `Nvidea.Core.Tests`, so the new regression can access the narrow internal factory seam.
- Static inspection confirms the production root still passes the evidence-decorated host into `BrowserGoalAgent`, while `BrowserHostRuntime` remains private to the root.
- The test is intentionally browser-free and payload-free.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local and intentionally non-durable; it is judge evidence, not an audit replacement.
- First-observation ledger semantics prevent retries, repeated approvals or repeated recovery from inflating proof.
- The shared ledger contains only a closed enum + timestamp projection; no prompts, URLs, browser text, memory content, approval scope, filenames, provider ids or raw errors are retained.
- Browser proof cannot be inferred from driver-reported success alone: terminal completion plus a trusted verified-step checkpoint is required.
- The goal-host decorator observes only after trusted-host calls return and cannot grant approval or alter durable job state.
- The composition regression does not weaken authority: it checks only the wrapper type and uses a passive host that cannot perform browser work.
- Existing browser authorization, durable job, audit, quarantine, emergency-stop and ambiguous-side-effect recovery authority is unchanged.
- Existing remote result/audit/cleanup/binding authority boundaries are unchanged.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Product-level and ambiguous-recovery evidence observation remain separate; ledger idempotence prevents proof inflation, but redundant observation should be removed only after equivalent host-level coverage is proven.
- The process ledger intentionally spans one desktop-process lifetime; a future in-app "new demo session" UX must explicitly clear it.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- `NebiusBackgroundExecutionObserved` is defined but not yet wired.

## Single Best Next Task
Remove redundant product/recovery evidence observation only where centralized host-level equivalence is proven, then wire `NebiusBackgroundExecutionObserved` from authenticated and reconciled remote lifecycle evidence. Keep that milestone fail-closed and payload-free: no optimistic dispatch, ambiguous delivery, unauthenticated callback, or unreconciled terminal state may count.
