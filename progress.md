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
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified. Added `ResearchSynthesisProvenance.Project` as the fail-closed boundary that converts untrusted model markers into verified evidence-backed provenance, then wired it into production `ResearchEngine.SynthesizeAsync` so `UsedCitations` can only come from verified Tavily evidence. Added a production `ResearchAsync` regression covering legitimate plus hallucinated markers and preservation of upstream provider warnings.

## Latest run — machine-readable research provenance
Files changed:
- `src/Nvidea.Core/Research/ResearchReportProvenance.cs`
- `tests/Nvidea.Core.Tests/ResearchReportProvenanceTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, `ResearchEngine`, `ResearchSynthesisProvenance`, and the existing production research regression before changing code.
- Added `ResearchProvenanceStatus` with explicit `Verified`, `Partial`, `Unverified`, and `NoSources` states so callers and judge tooling no longer need to parse human warning strings.
- Added `ResearchReportProvenance.FromReport`, which derives status only through the deterministic `ResearchSynthesisProvenance`/citation-integrity boundary. It never promotes model text or warnings into provenance authority.
- Kept no-source reports semantically distinct from synthesized-but-uncited reports, avoiding the misleading claim that a search returning no evidence is equivalent to an answer that failed to cite available evidence.
- Preserved unknown/fabricated source IDs as machine-readable diagnostics for partial provenance.
- Added provider-free regression coverage for fully verified, partially verified with fabricated marker, uncited/unverified, and no-source reports.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms `FromReport` delegates synthesis verification to `ResearchSynthesisProvenance.Project`, which in turn delegates source-marker authority to `ResearchCitationIntegrity.Verify`.
- The new tests require all four status classes and explicitly preserve the fabricated source id in the partial case.
- No live provider, network research request, browser, API key, paid service, or workflow was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Machine-readable status is derived from evidence identity, not warning text, preventing UI/evaluator code from treating model-authored prose as verification metadata.
- Fabricated source IDs remain diagnostics only and never become verified citations.
- No-source and uncited-answer states fail closed rather than being presented as verified.
- The projection adds no persistence of evidence bodies, credentials, prompts, authorization material, or browser state.
- Prompt-injection boundaries, consequential-action approval semantics, cancellation, and browser lifetime behavior are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- `ResearchReportProvenance` is currently an explicit deterministic projection over `ResearchReport`, rather than a serialized field embedded in the report contract; this preserves backward compatibility but judge-visible evidence still needs to consume the projector.
- The Windows judge-evidence surface does not yet display this machine-readable research provenance state.

## Single Best Next Task
Propagate `ResearchReportProvenance.FromReport` into the judge-visible research evidence/presentation path and deterministic demo evaluator, showing Verified/Partial/Unverified/NoSources plus unknown source IDs without parsing warnings. Add regressions proving fabricated markers cannot produce a green/verified judge state.
