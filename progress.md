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
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified. Added `ResearchSynthesisProvenance.Project` as the fail-closed boundary that converts untrusted model markers into verified evidence-backed provenance, then wired it into production `ResearchEngine.SynthesizeAsync` so `UsedCitations` can only come from verified Tavily evidence.

## Latest run — production research provenance integration regression
Files changed:
- `tests/Nvidea.Core.Tests/ResearchEngineTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, production `ResearchEngine.SynthesizeAsync`, and the existing ResearchEngine regression before changing code.
- Strengthened the full `ResearchAsync` integration regression so deterministic fake Nemotron synthesis emits both a legitimate `[src:s1]` marker and hallucinated `[src:fake]` marker while the prepared Tavily batch also carries an upstream provider/freshness warning.
- The regression now requires exactly one verified citation (`s1`), explicitly forbids `fake` from `UsedCitations`, requires the hallucinated-id diagnostic, and independently requires the upstream warning to survive production provenance projection.
- The test still drives the complete production plan -> provider evidence -> deterministic ranking -> synthesis path and verifies that deterministic quality metadata and the untrusted-web-evidence boundary are supplied to inference.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the exercised production path is `ResearchAsync` -> `PlanAsync` -> `GatherEvidenceAsync` -> `SynthesizeAsync` -> `ResearchSynthesisProvenance.Project`.
- The regression uses no live provider, network, browser, API key, or paid service and deterministically distinguishes upstream provider warnings from model-hallucination warnings.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Nemotron synthesis remains untrusted text; model-formatted source IDs cannot manufacture verified citations.
- Provider warnings are preserved through provenance projection, preventing integrity diagnostics from masking Tavily freshness/provider warnings.
- The regression explicitly guards against a future refactor accidentally reintroducing hallucinated IDs into `UsedCitations`.
- No evidence bodies, credentials, prompts, authorization material, or browser state were newly persisted or transmitted.
- Prompt-injection boundaries, consequential-action approval semantics, cancellation, and browser lifetime behavior are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- `ResearchReport` exposes verified citations and warnings but not a first-class machine-readable provenance status; judge evidence currently has to infer full verification from citations/warnings.
- The no-source early return has intentionally different semantics from synthesis provenance and should remain explicit if report status is added.

## Single Best Next Task
Add a backward-compatible machine-readable provenance result to `ResearchReport` (for example verified/partial/unverified plus unknown source IDs) derived only from `ResearchSynthesisProvenance`, propagate it into judge-visible evidence, and add regression coverage for fully verified, partially verified/fabricated, uncited, and no-source reports. This will let the demo/evaluator prove citation integrity without parsing human warning strings.
