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
Added deterministic citation verification, fail-closed synthesis provenance, Verified/Partial/Unverified/NoSources report state, strict judging authority, payload-safe `ResearchJudgeEvidence`, serialization privacy coverage, production synthesis integration, and deterministic evaluator integration. Mixed legitimate/fabricated markers are explicitly Partial and cannot turn evaluator research evidence green.

## Latest run — Windows research provenance presentation
Files changed:
- `src/Nvidea.Core/Desktop/DesktopResearchJudgePresentation.cs`
- `tests/Nvidea.Core.Tests/DesktopResearchJudgePresentationTests.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs`
- `src/Nvidea.Windows/MainWindow.Readiness.cs`
- `progress.md`

Completed:
- Added a Core-owned, payload-free Windows presentation projection over canonical `ResearchJudgeEvidence`.
- Presentation is fail-closed: only canonical `verified` + `IsVerifiedForJudging=true` + zero unknown source IDs can render `RESEARCH PROVENANCE: VERIFIED`.
- Partial, Unverified, NoSources, missing evidence, and any fabricated/unknown source ID render NOT VERIFIED.
- Added provider-free presentation regressions for verified, every non-verified state, missing evidence, and an adversarial caller claiming verified while supplying a fabricated source ID.
- Added a dedicated Research provenance panel to `JudgeEvidenceDialog`; it shows only status, evidence-source/verified-citation counts, and unknown IDs.
- `MainWindow.Readiness` now reads a completed report through the existing product boundary and immediately collapses it through `ResearchJudgeEvidence.FromReport` and `DesktopResearchJudgePresentation`; raw question/answer/source payloads are never passed into the dialog.
- Failures reading completed/canonical provenance remain non-fatal and fail closed to NOT VERIFIED.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the Windows surface consumes the same production `ResearchJudgeEvidence` authority used by the evaluator, not an independent marker parser.
- Core regression source covers the green-state invariant and fabricated-ID fail-closed behavior without provider/network dependencies.
- No live provider, browser, workflow, API key, paid service, or other repository was touched.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Judge research presentation has no fields for question text, synthesis, source bodies, URLs, titles, queries, credentials, provider IDs, or desktop context.
- Fabricated IDs are diagnostic only and force NOT VERIFIED even if a caller incorrectly supplies a true verification boolean.
- Missing/incomplete/corrupt/concurrently changing completed-report state fails closed without breaking the rest of the evidence dialog.
- Existing prompt-injection, consequential-action approval, cancellation, browser lifetime, provider routing, and remote execution boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- `ReadCompletedReportAsync` currently requires the local research runtime; a completed remote report may therefore show durable lineage but NOT VERIFIED canonical research provenance on a desktop lacking local Tavily configuration. This is safe but should be improved with a payload-free completed-report provenance read owned by the durable runtime.
- Evaluator schema-v2 consumers/documentation should be reviewed before release qualification.

## Single Best Next Task
Add a payload-free durable `ResearchJudgeEvidence` read to `ResearchProductRuntime` so completed local or remotely-ingested research can expose canonical provenance to the Windows judge surface without requiring local Tavily execution availability or materializing the raw report in WPF. Add local/remote and corrupt/incomplete fail-closed regressions.
