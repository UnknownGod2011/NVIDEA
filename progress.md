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
Added deterministic `ResearchCitationIntegrity` verification for Nemotron-emitted `[src:SOURCE_ID]` markers. Verification is evidence-backed, case-insensitive, deduplicated, reports fabricated IDs, and treats uncited synthesis as unverified. Added `ResearchSynthesisProvenance.Project` as the fail-closed boundary that converts untrusted model markers into verified evidence-backed provenance.

## Latest run — production research provenance integration
Files changed:
- `src/Nvidea.Core/Research/ResearchEngine.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, the full production `ResearchEngine.SynthesizeAsync` path, and `ResearchSynthesisProvenance` before changing code.
- Removed the production engine's duplicated inline regex/source-id projection and its direct construction of `UsedCitations` from model-emitted markers.
- Wired `ResearchSynthesisProvenance.Project(answer, batch.Citations, batch.Warnings)` directly into `SynthesizeAsync`.
- `ResearchReport.UsedCitations` now derives exclusively from `provenance.VerifiedCitations`; fabricated/unknown model markers cannot be promoted into judge/user-visible verified Tavily provenance.
- `ResearchReport.Warnings` now derives from the same fail-closed projection, preserving provider warnings while explicitly identifying unknown markers or uncited synthesis.
- Removed the now-unused `System.Text.RegularExpressions` dependency from `ResearchEngine` so there is one canonical marker-validation implementation.
- Repository identity was explicitly reverified immediately before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the production synthesis return path has a single provenance authority: `ResearchSynthesisProvenance.Project` -> `ResearchCitationIntegrity.Verify`.
- The previously added provider/network-free provenance tests cover mixed valid+fabricated markers, uncited synthesis, and case-insensitive fully verified synthesis; production now consumes that tested projection.
- GitHub reports no commit status checks for the implementation commit; no workflow was triggered manually and no expensive CI/artifact operation was requested.
- No API keys, provider calls, browser processes, workflow reruns, or external repository writes were used.
- Executable PASS is not claimed because this environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Nemotron synthesis remains untrusted text; it cannot create a verified citation by formatting a plausible `[src:...]` marker.
- Verified citations retain only citation objects already supplied by the Tavily-backed evidence batch; unknown IDs remain diagnostics only.
- Existing upstream Tavily warnings are preserved through projection rather than overwritten.
- No evidence bodies, credentials, prompts, authorization material, or browser state were newly persisted or transmitted.
- Prompt-injection boundaries, consequential-action approval semantics, cancellation, and browser lifetime behavior are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Production provenance wiring is statically reviewed but cannot be compiled here; first capable environment must run focused Research tests and fix any signature mismatch without weakening fail-closed semantics.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- `ResearchReport` currently exposes verified citations and warnings but not a first-class `IsFullyVerified`/unknown-ID field; judge evidence may benefit from an explicit machine-readable provenance status instead of deriving it from warnings.

## Single Best Next Task
Add ResearchEngine-level integration regression coverage around `SynthesizeAsync` using deterministic fake inference/prepared Tavily evidence, proving a Nemotron answer containing one legitimate and one hallucinated source ID returns only the legitimate `UsedCitation`, preserves upstream warnings, and emits the unverified-ID warning. Then expose an explicit machine-readable provenance status in the report/judge-evidence path if it can be done compatibly, before continuing cross-subsystem deterministic demo qualification.
