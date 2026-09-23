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
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified.

## Latest run — fail-closed synthesis provenance projection
Files changed:
- `src/Nvidea.Core/Research/ResearchSynthesisProvenance.cs`
- `tests/Nvidea.Core.Tests/ResearchSynthesisProvenanceTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, `ResearchEngine.SynthesizeAsync`, `ResearchCitationIntegrity`, and Tavily citation contracts before changing code.
- Added `ResearchSynthesisProvenance.Project`, a deterministic boundary that converts untrusted model markers into the only citation collection allowed to be described as verified provenance.
- Fabricated source IDs are excluded from `VerifiedCitations`, retained explicitly as `UnknownSourceIds`, and produce a durable warning saying they were excluded from verified Tavily provenance.
- Uncited synthesis produces zero verified citations and an explicit warning; fully evidence-backed markers retain a positive `IsFullyVerified` signal.
- Existing provider warnings are preserved rather than overwritten.
- Added provider/network-free regression tests for mixed valid+fabricated markers, uncited synthesis, and case-insensitive fully verified synthesis.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms projection delegates marker parsing and evidence matching solely to `ResearchCitationIntegrity`; it cannot manufacture citations from model text.
- Tests construct real `ResearchCitation` records and assert fabricated IDs never enter verified provenance.
- No API keys, provider calls, browser processes, workflow runs, or external repository writes were used.
- Executable PASS is not claimed because this environment cannot run the .NET 8/Windows suite; direct git clone also failed because the container cannot resolve GitHub, so connector-backed static validation was used.

## Security / privacy / failure review
- Projection is deterministic/local and does not persist or transmit evidence bodies, URLs, credentials, or prompts.
- Unknown model markers fail closed for provenance: they remain diagnostics only and never become verified citations.
- Existing Tavily/Nemotron prompt-injection boundaries and browser approval semantics are unchanged.
- Important remaining gap: `ResearchEngine.SynthesizeAsync` still uses its older inline marker/warning projection. The new production-quality projection exists and is tested, but must be wired into that return path before judge-visible `ResearchReport` provenance is guaranteed to use it.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- New provenance tests are statically reviewed but cannot be compiled here; first capable environment must run focused Core tests and fix signature mismatches without weakening fail-closed semantics.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Replace the inline marker parsing in `ResearchEngine.SynthesizeAsync` with `ResearchSynthesisProvenance.Project`, making `ResearchReport.UsedCitations` derive exclusively from verified evidence-backed citations and its warnings from the fail-closed projection. Add/extend ResearchEngine integration tests proving a hallucinated source ID can never be presented as verified Tavily provenance, then continue cross-subsystem deterministic demo qualification.
