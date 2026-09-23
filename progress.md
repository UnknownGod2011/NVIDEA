# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, authoritative serialized browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-21 — product foundation and hardening
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling. Hardened browser transport, prompt-injection/consequential-action gates, emergency stop, protected browser receipts, durable research lineage and crash-ambiguous reconciliation.

### 2026-09-22 to 2026-09-23 — composition lifetime and browser-goal qualification
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, and moved browser product/recovery/goal facades behind root-lifetime authority. Added `IBrowserGoalAgent`, exactly-one-lease transaction semantics, a private least-authority browser-host seam, and an assembly-internal deterministic root factory. Locked public API boundaries and qualified actual-root shutdown behavior for Run, Resume, Approve-and-Continue, and Cancel, including stale-facade fail-closed behavior and authority counters.

### 2026-09-23 — research citation integrity
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified. Added `ResearchSynthesisProvenance.Project` as the fail-closed boundary that converts untrusted model markers into verified evidence-backed provenance, then wired it into production `ResearchEngine.SynthesizeAsync` so `UsedCitations` can only come from verified Tavily evidence. Added a production `ResearchAsync` regression covering legitimate plus hallucinated markers and preservation of upstream provider warnings. Added `ResearchReportProvenance` with explicit Verified/Partial/Unverified/NoSources states and a strict `IsVerifiedForJudging` gate. Added payload-safe `ResearchJudgeEvidence` for evaluator/UI surfaces plus serialization privacy coverage proving question/answer/source payloads do not cross that boundary.

## Latest run — evaluator provenance authority integration
Files changed:
- `tools/Nvidea.PersonalAiDemoEval/Program.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, the current evaluator, `ResearchJudgeEvidence`, and the `ResearchReport` contract before changing code.
- Replaced the evaluator's older inline `cited-research` predicate with `ResearchJudgeEvidence.FromReport(report)` as the single research judging authority.
- `cited-research` now requires the canonical projection to be fully `Verified`, contain exactly two verified citations/evidence sources, and contain zero unknown source IDs.
- Added an evaluator-level adversarial mixed-citation fixture by cloning the real deterministic report and appending `[src:fabricated-demo-source]`. The evaluator now requires that projection to become `partial`, remain non-verified, retain the two legitimate verified citations, and surface the fabricated ID diagnostically.
- Added the adversarial result as its own required evaluator check, so a regression that accidentally trusts fabricated provenance makes the overall deterministic demo evaluation fail.
- Added the payload-safe `ResearchJudgeEvidence` projection directly to serialized evaluator evidence and bumped the evaluator evidence schema from 1 to 2. The serialized research object exposes only provenance state, strict verification authority, counts, and unknown IDs.
- Metrics now derive research source/citation counts from the canonical projection rather than independently counting report collections.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the evaluator references `Nvidea.Core` directly and therefore consumes the production `ResearchJudgeEvidence` type rather than a duplicate evaluator-local implementation.
- The deterministic happy-path fixture can only make `cited-research` green through the production fail-closed provenance projection.
- The deterministic adversarial fixture explicitly demonstrates that one fabricated marker alongside two legitimate markers cannot produce a green research authority state.
- No live provider, network research request, browser, API key, paid service, or workflow was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Fabricated source IDs remain diagnostics only and now actively fail the deterministic evaluator's research verification authority.
- Partial, uncited, and no-source states remain fail-closed.
- Serialized evaluator research evidence uses the payload-safe projection; source bodies, question text, synthesized answer text, source URLs/titles/queries, credentials, and private desktop context are not fields on that projection.
- Prompt-injection boundaries, consequential-action approval semantics, cancellation, browser lifetime behavior, provider routing, and remote research execution are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The Windows judge-evidence surface still does not display the machine-readable research provenance label/unknown IDs.
- Evaluator schema consumers/documentation that assume schema version 1 should be reviewed and updated before release qualification.

## Single Best Next Task
Propagate `ResearchJudgeEvidence` into the Windows judge-evidence surface (`JudgeEvidenceDialog` / research presentation) with a clear Verified/Partial/Unverified/NoSources status and fabricated-ID diagnostics while preserving the payload-safe boundary. Add presentation-level regressions proving Partial/Unverified can never render as a green verified research state.
