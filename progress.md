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
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified. Added `ResearchSynthesisProvenance.Project` as the fail-closed boundary that converts untrusted model markers into verified evidence-backed provenance, then wired it into production `ResearchEngine.SynthesizeAsync` so `UsedCitations` can only come from verified Tavily evidence. Added a production `ResearchAsync` regression covering legitimate plus hallucinated markers and preservation of upstream provider warnings. Added `ResearchReportProvenance` with explicit Verified/Partial/Unverified/NoSources states and a strict `IsVerifiedForJudging` gate.

## Latest run — payload-safe research judge evidence
Files changed:
- `src/Nvidea.Core/Research/ResearchJudgeEvidence.cs`
- `tests/Nvidea.Core.Tests/ResearchJudgeEvidenceTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, `ResearchReportProvenance`, and the deterministic Personal AI demo evaluator before changing code.
- Added `ResearchJudgeEvidence`, a small payload-safe projection intended for evaluator JSON and Windows judge surfaces. It exposes only stable provenance label, strict verified boolean, evidence-source count, verified-citation count, and unknown/fabricated source IDs; it does not expose source bodies, prompts, credentials, or private context.
- The projection delegates green/verified authority to `ResearchReportProvenance.IsVerifiedForJudging` and independently derives `VerifiedCitationCount` from `ResearchSynthesisProvenance.Project`, not from the report's supplied citation collection. This prevents an uncited answer from appearing stronger merely because citation metadata exists upstream.
- Added provider-free regressions for fully verified, mixed legitimate+fabricated, and uncited-with-sources cases. The mixed case remains `partial`/not verified and preserves `fake`; the uncited case reports zero verified citations even when evidence/citation metadata exists.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the new judge projection obtains provenance authority only through the existing deterministic citation-integrity chain.
- Regression fixtures explicitly separate available evidence from actually cited/verified evidence, reducing the risk of evaluator false-greens.
- No live provider, network research request, browser, API key, paid service, or workflow was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Fabricated source IDs remain diagnostics only and cannot make judge evidence green.
- Partial, uncited, and no-source states remain fail-closed.
- Judge evidence contains identifiers/counts only; no research snippets/source bodies or private desktop context are projected.
- Prompt-injection boundaries, consequential-action approval semantics, cancellation, browser lifetime behavior, and provider routing are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- `Nvidea.PersonalAiDemoEval` still uses its older inline `cited-research` predicate; the new `ResearchJudgeEvidence` object is now the canonical ready-to-wire projection but is not yet serialized by that tool.
- The Windows judge-evidence surface likewise does not yet display the machine-readable provenance label/unknown IDs.

## Single Best Next Task
Wire `ResearchJudgeEvidence.FromReport(report)` into `Nvidea.PersonalAiDemoEval`: serialize the projection into evidence JSON and make `cited-research` depend on its strict `Verified` gate. Add evaluator-level regression/fixture coverage proving a synthesis containing one legitimate marker plus one fabricated marker cannot produce an overall green research check.
