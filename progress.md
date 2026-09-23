# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, authoritative research provenance, serialized browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-21 — product foundation and hardening
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling. Hardened browser transport, prompt-injection/consequential-action gates, emergency stop, protected browser receipts, durable research lineage and crash-ambiguous reconciliation.

### 2026-09-22 to 2026-09-23 — composition lifetime and browser-goal qualification
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, and moved browser product/recovery/goal facades behind root-lifetime authority. Added `IBrowserGoalAgent`, exactly-one-lease transaction semantics, a private least-authority browser-host seam, and an assembly-internal deterministic root factory. Locked public API boundaries and qualified actual-root shutdown behavior for Run, Resume, Approve-and-Continue, and Cancel, including stale-facade fail-closed behavior and authority counters.

### 2026-09-23 — research citation integrity and evaluator authority
Added deterministic citation verification, fail-closed synthesis provenance, Verified/Partial/Unverified/NoSources report state, strict judging authority, payload-safe `ResearchJudgeEvidence`, serialization privacy coverage, production synthesis integration, deterministic evaluator integration, and Windows judge presentation. Mixed legitimate/fabricated markers are explicitly Partial and cannot turn evaluator or Windows research evidence green.

## Latest run — provider-free durable research provenance
Files changed:
- `src/Nvidea.Core/Jobs/ResearchJobHandler.cs`
- `src/Nvidea.Core/Jobs/ResearchProductRuntime.cs`
- `src/Nvidea.Windows/MainWindow.Readiness.cs`
- `tests/Nvidea.Core.Tests/ResearchProductJudgeEvidenceTests.cs`
- `progress.md`

Completed:
- Added `ResearchJobHandler.ReadCompletedJudgeEvidence`, projecting canonical `ResearchJudgeEvidence` directly at the durable completed-checkpoint boundary.
- Added `ResearchProductRuntime.ReadCompletedJudgeEvidenceAsync`, which reads durable state through `JsonAgentJobStore` and does not require `ILocalResearchRuntime`, Tavily credentials, Nemotron provider availability, or cloud execution availability.
- The path works equally for locally completed and remotely ingested completed checkpoints because authority comes from the durable result itself after ingestion.
- Windows judge readiness now calls the payload-free durable evidence API instead of `ReadCompletedReportAsync`; WPF never receives the raw question, answer, source bodies, URLs, titles, or queries.
- Added provider-free regression source proving a completed NebiusServerless record can expose Verified canonical evidence with `local: null`, plus corrupt completed-checkpoint fail-closed coverage.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the new product API has no dependency on `RequireLocal()` and delegates canonical authority to `ResearchJudgeEvidence.FromReport` at the Core durable boundary.
- The Windows call site no longer imports or materializes `ResearchReport`/`ResearchJudgeEvidence`; it consumes only the safe projection returned by Core.
- Regression source constructs a durable remotely located completed job, instantiates `ResearchProductRuntime` with no local provider runtime, and requires canonical verified counts/state.
- Corrupt completed payload throws and the existing Windows catch path renders NOT VERIFIED.
- No live provider, browser, workflow, API key, paid service, or other repository was touched.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- The public durable evidence API returns only provenance label, strict verification boolean, evidence/verified-citation counts, and unknown source IDs through `ResearchJudgeEvidence`.
- Raw durable report payload is deserialized only inside Core long enough to derive canonical evidence; it does not cross into WPF or judge JSON presentation.
- Missing, incomplete, legacy-invalid, corrupt, or concurrently changing completed state fails closed rather than synthesizing optimistic evidence.
- Remote ingestion remains responsible for its existing authenticity/exact-once trust checks before a result can become a completed durable checkpoint; this change does not bypass those controls.
- Existing prompt-injection, consequential-action approval, cancellation, browser lifetime, provider routing, and remote execution boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The new regression source is not executable in this connector environment; compile/runtime compatibility must be confirmed on a .NET 8 runner before release qualification.
- Evaluator schema-v2 consumers/documentation should be reviewed before release qualification.
- Durable provenance is derived from the persisted report rather than separately signed as its own artifact; authenticity therefore inherits the completed-checkpoint/remote-ingestion trust boundary.

## Single Best Next Task
Harden the durable research provenance boundary against checkpoint/report tampering by binding canonical `ResearchJudgeEvidence` (or a deterministic provenance digest) into `DurableResearchReceipt`, verifying that binding on read, and adding tamper regressions where report citation markers/evidence are altered after completion. This will make judge-visible provenance cryptographically consistent with the existing plan/evidence/synthesis receipt chain rather than merely structurally derived from durable state.
