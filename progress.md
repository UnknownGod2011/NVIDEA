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

## Latest run — research citation provenance integrity
Files changed:
- `src/Nvidea.Core/Research/ResearchCitationIntegrity.cs`
- `tests/Nvidea.Core.Tests/ResearchCitationIntegrityTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md` and the production `ResearchEngine`, Tavily research contracts, evidence-quality model, and Nemotron inference contract before selecting work.
- Moved beyond lifetime-only qualification toward the judge-demo evidence chain by adding a deterministic `ResearchCitationIntegrity` verifier for model-emitted `[src:SOURCE_ID]` markers.
- The verifier derives provenance only from the actual `ResearchCitation` collection supplied by Tavily-backed prepared evidence; model output cannot create a verified citation merely by formatting a marker.
- Verification is case-insensitive for source identity, deduplicates repeated markers, returns only evidence-backed citations, explicitly reports fabricated/unknown source IDs, and treats an uncited answer as not fully verified.
- Added provider/network-free tests covering valid markers, a mixed valid+fabricated answer, repeated case-variant markers, and uncited synthesis.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the verifier accepts only the same marker grammar already used by `ResearchEngine` and maps markers solely against concrete `ResearchCitation.SourceId` values.
- No API keys, provider calls, browser processes, workflow runs, or external writes were used.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows test suite.

## Security / privacy / failure review
- Citation verification is deterministic and local; no evidence content, URLs, credentials, or prompts are persisted or transmitted by the verifier.
- Unknown source markers fail the `IsFullyVerified` signal and are never returned as verified citations.
- This closes the reusable verification primitive but production `ResearchEngine.SynthesizeAsync` still has its older inline marker parsing/warning path; until the new verifier is wired there, the final report can still carry an unknown marker alongside a warning.
- Existing prompt-injection defenses, Tavily evidence isolation, freshness/authority heuristics, browser approval boundaries, and composition shutdown semantics are unchanged.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- The citation-integrity tests are statically reviewed but cannot be compiled here; first capable environment must run focused Core tests and fix any signature mismatch without weakening fail-closed semantics.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Wire `ResearchCitationIntegrity.Verify` into `ResearchEngine.SynthesizeAsync` so production reports derive `UsedCitations` and citation warnings from the deterministic verifier, then make fabricated markers fail closed in judge-visible evidence instead of merely relying on prompt compliance. Add integration tests proving a hallucinated source ID can never be presented as verified Tavily provenance. After that, continue the cross-subsystem deterministic demo qualification and run the full .NET 8/Windows suite in the first capable environment.
