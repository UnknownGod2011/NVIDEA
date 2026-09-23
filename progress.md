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

### 2026-09-24 — durable provenance and remote transport integrity
Extended completed research receipts with SHA-256 commitments over the canonical full `ResearchReport` and payload-free `ResearchJudgeEvidence`; provider-free judge reads verify both commitments in fixed time and reject legacy/unbound or tampered checkpoints. Added remote-result AEAD adversarial qualification proving ciphertext mutation and ciphertext/tag substitution cannot alter a completed checkpoint (including its report/receipt payload) while retaining a different envelope's authenticated provenance.

## Latest run — remote-result payload substitution qualification
Files changed:
- `tests/Nvidea.Core.Tests/NebiusResearchResultProtocolTests.cs`
- `progress.md`

Completed:
- Re-read the remote-result protocol and ingestion boundary after the new completed-receipt commitments landed.
- Confirmed the encrypted result envelope authenticates the entire serialized `RemoteResearchStageResult`, including `JobStepResult.CheckpointPayload`, while its AEAD associated data independently binds opaque work-item id, remote-job id, completion time and expiry.
- Added an adversarial regression that flips one ciphertext bit in a completed research result carrying report/receipt-shaped checkpoint data; decryption must fail cryptographically before ingestion can observe substituted payload.
- Added a second adversarial regression that takes ciphertext + authentication tag from another independently valid envelope and inserts them into the expected envelope; the different nonce/associated authenticated context must cause cryptographic rejection.
- Preserved the existing remote-job-id and opaque-work-item substitution regressions, so metadata and payload substitution are now represented together.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection of `ResearchResultProtector` confirms AES-256-GCM authenticates the serialized stage result and binds result provenance as associated data; the checkpoint payload therefore cannot be modified in transit without invalidating the tag.
- Static inspection of `RemoteResearchResultIngestor` confirms unprotection/provenance validation happens before the remote `JobStepResult` becomes the durable local output checkpoint.
- The new regressions are provider-free and contain no credentials or live service calls.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Completed report/provenance commitments remain one-way SHA-256 values; no raw question, answer, source body, URL, query or desktop context is added to judge evidence.
- Remote result confidentiality/integrity uses RSA-OAEP-SHA256 wrapped AES-256-GCM; ciphertext mutation or envelope-context substitution is fail-closed.
- Important trust distinction: encryption to the client's public key authenticates ciphertext integrity but does NOT by itself prove worker identity, because possession of the client public key is not a signing authority. Do not describe the current result envelope as worker-signed.
- The durable local receipt commitments protect report/provenance consistency after completion; remote authenticity still needs an explicit worker-signature verification boundary if worker identity must be cryptographically proven independent of transport/storage trust.
- Existing prompt-injection, consequential-action approval, cancellation, browser lifetime, provider routing and emergency-stop boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- New regression source is not executable in this connector environment; compile/runtime compatibility must be confirmed on a .NET 8 runner before release qualification.
- Existing completed checkpoints created before the bound-receipt version intentionally cannot claim judge-verified research provenance; rerunning research is required.
- The remote result envelope currently provides authenticated encryption to the client but no explicit worker digital signature. A party that possesses the client's public encryption key could construct a fresh envelope; local dispatch provenance checks narrow substitution, but they are not equivalent to worker-origin authentication.

## Single Best Next Task
Add explicit worker-origin signatures to the protected remote-result protocol: have the worker sign a canonical commitment covering envelope provenance plus the encrypted result/report-receipt payload, pin the worker public verification key on the client, verify the signature before decryption/ingestion, and add adversarial regressions for forged signatures, report/receipt substitution, cross-job replay and key mismatch. This closes the remaining distinction between AEAD integrity and cryptographic worker authenticity.
