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
Added deterministic citation verification, fail-closed synthesis provenance, Verified/Partial/Unverified/NoSources report state, strict judging authority, payload-safe `ResearchJudgeEvidence`, serialization privacy coverage, production synthesis integration, deterministic evaluator integration, Windows judge presentation, and provider-free durable evidence reads. Mixed legitimate/fabricated markers are explicitly Partial and cannot turn evaluator or Windows research evidence green.

## Latest run — cryptographically bound durable research provenance
Files changed:
- `src/Nvidea.Core/Jobs/ResearchJobHandler.cs`
- `tests/Nvidea.Core.Tests/ResearchProductJudgeEvidenceTests.cs`
- `progress.md`

Completed:
- Extended `DurableResearchReceipt` with optional `ReportSha256` and `ProvenanceSha256` commitments while retaining deserialization compatibility for older receipts.
- New completed research checkpoints commit SHA-256 over the entire canonical serialized `ResearchReport`, covering answer/citation/evidence mutations, plus a separate deterministic payload-free digest over `ResearchJudgeEvidence`.
- `ReadCompletedJudgeEvidence` now verifies both bindings in fixed-time before returning judge-visible provenance. Missing legacy bindings, report tampering, or provenance mismatch fail closed.
- The provenance digest uses an explicit version domain, invariant numeric formatting, stable unknown-ID ordering and a separator-safe canonical representation rather than relying on presentation strings or JSON property ordering.
- Reworked provider-free remote evidence regression to produce a real completed checkpoint through `ResearchJobHandler` before persisting it as `NebiusServerless`, so the test exercises the same receipt-generation path as production.
- Added adversarial regressions for post-completion citation-marker tampering, source-body tampering, legacy receipts without the new bindings, and corrupt checkpoints.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms judge evidence is returned only after full-report and canonical-provenance fingerprints match the receipt.
- Tampering fixtures alter the completed payload while leaving the original receipt unchanged; the read boundary must reject before any evidence can be shown as verified.
- Existing remote/provider-free behavior remains represented by constructing the completed checkpoint with real Core logic and then reading it with `ResearchProductRuntime(local: null)`.
- No live provider, browser, workflow, API key, paid service, or other repository was touched.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- The added receipt fields are one-way SHA-256 commitments only; raw question, synthesis, source body, URL, title, query and desktop context are not added to judge evidence.
- `ReadCompletedJudgeEvidence` fails closed for legacy receipts instead of silently trusting unbound provenance. Raw report access remains separately available for product display and is not upgraded to judge authority by this change.
- Full-report commitment catches source-body, citation metadata and synthesis mutations; the separate provenance commitment prevents a future projection change or partial payload manipulation from silently changing judge authority.
- Fixed-time digest comparison is retained through the existing `CryptographicOperations.FixedTimeEquals` path.
- Existing prompt-injection, consequential-action approval, cancellation, browser lifetime, provider routing, remote signature/exact-once ingestion and emergency-stop boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- New regression source is not executable in this connector environment; compile/runtime compatibility must be confirmed on a .NET 8 runner before release qualification.
- Existing completed checkpoints created before this receipt version intentionally cannot claim judge-verified research provenance; rerunning research is required to produce bound evidence.
- Receipt commitments are integrity bindings inside the completed checkpoint, not an independent signature. For remotely ingested results, authenticity still depends on the existing signed result-envelope/exact-once ingestion trust boundary.

## Single Best Next Task
Bind the new `ReportSha256`/`ProvenanceSha256` commitments into the signed remote-result ingestion verification path and add a remote-envelope adversarial regression proving a worker/result payload cannot substitute a report or receipt independently. This will connect completed-checkpoint provenance integrity directly to the existing remote authenticity boundary rather than relying on post-ingestion co-location alone.
