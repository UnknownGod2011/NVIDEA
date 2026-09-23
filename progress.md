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
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified. Added `ResearchSynthesisProvenance.Project` as the fail-closed boundary that converts untrusted model markers into verified evidence-backed provenance, then wired it into production `ResearchEngine.SynthesizeAsync` so `UsedCitations` can only come from verified Tavily evidence. Added a production `ResearchAsync` regression covering legitimate plus hallucinated markers and preservation of upstream provider warnings. Added `ResearchReportProvenance` with explicit Verified/Partial/Unverified/NoSources states.

## Latest run — fail-closed judge provenance gate
Files changed:
- `src/Nvidea.Core/Research/ResearchReportProvenance.cs`
- `tests/Nvidea.Core.Tests/ResearchReportProvenanceTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, the provenance projector, and the deterministic Personal AI demo evaluator before changing code.
- Added `ResearchReportProvenance.IsVerifiedForJudging`, a deliberately strict boolean gate that is true only for `Verified`. Judge/demo code no longer needs to rely on enum ordering, warning text, or model-authored prose to decide whether research provenance may be presented as green/verified.
- Added stable `JudgeLabel` values (`verified`, `partial`, `unverified`, `no-sources`) for evidence JSON/UI without exposing warning-string parsing as an authority boundary.
- Extended provider-free regression coverage so mixed legitimate + fabricated source markers are explicitly `Partial`, preserve the fabricated source ID, and fail the judge gate.
- Added a fail-closed theory proving every non-Verified state (`Partial`, `Unverified`, `NoSources`) is rejected by `IsVerifiedForJudging`.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the judge gate consumes only `ResearchProvenanceStatus`, whose status is derived through `ResearchSynthesisProvenance.Project` and `ResearchCitationIntegrity.Verify`.
- Tests now make it structurally difficult for future evaluator/UI code to accidentally treat partial provenance as verified merely because at least one citation is legitimate.
- No live provider, network research request, browser, API key, paid service, or workflow was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Fabricated source IDs remain diagnostics only and cannot make the judge gate green.
- Partial provenance is explicitly fail-closed even when some citations are legitimate.
- No-source and uncited-answer states remain distinct and both fail the judge gate.
- Labels are deterministic local metadata; no evidence bodies, prompts, credentials, authorization material, or private browser state are added to persistence.
- Prompt-injection boundaries, consequential-action approval semantics, cancellation, and browser lifetime behavior are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The deterministic `Nvidea.PersonalAiDemoEval` still uses its older `cited-research` predicate and does not yet serialize `ResearchReportProvenance` into its evidence JSON; the new gate is ready for that wiring but is not yet judge-visible in the emitted artifact.
- The Windows judge-evidence surface likewise does not yet display the machine-readable provenance label/unknown IDs.

## Single Best Next Task
Wire `ResearchReportProvenance.FromReport(report)` into `Nvidea.PersonalAiDemoEval` evidence JSON and its `cited-research` pass/fail predicate, serializing the stable label plus unknown source IDs. Add evaluator-level regression coverage proving a deterministic synthesis containing one legitimate marker plus one fabricated marker cannot produce an overall green research check.
