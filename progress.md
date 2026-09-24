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

### 2026-09-22 to 2026-09-23 — composition lifetime, browser qualification, citation authority
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, integrated `CompositionLifetimeGate`, and moved browser facades behind root-lifetime authority. Added exactly-one-lease browser goal semantics and qualified shutdown behavior. Added deterministic research citation verification, Verified/Partial/Unverified/NoSources authority, payload-safe `ResearchJudgeEvidence`, evaluator/Windows presentation, and provider-free durable evidence reads.

### 2026-09-24 — durable provenance and remote-result authentication migration
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, and worker-side signing after AEAD protection.

## Latest run — dedicated worker result-signing secret trust
Files changed:
- `src/Nvidea.Core/Jobs/WorkerResultSigningPrivateKeyTrust.cs`
- `tests/Nvidea.Core.Tests/WorkerResultSigningPrivateKeyTrustTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, worker result protocol/signature code, actual `RemoteResearchResultIngestor`, worker runtime configuration, and worker process composition before changing code.
- Added a dedicated worker-only secret boundary for `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM`; it requires an RSA private identity of at least 2048 bits and canonicalizes it before use.
- Kept the result-signing secret role explicitly separate from `NVIDEA_WORKER_PRIVATE_KEY_PEM`, preventing accidental reuse of the work-item decryption identity.
- Added provider-free regressions proving the loader reads only the dedicated signing secret and rejects missing, public-only, and undersized RSA identities.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the new trust boundary has no provider/network dependency and returns only validated private-key material.
- Tests exercise secret-name isolation and key-role validity without real credentials.
- Existing worker signing occurs after AEAD protection; existing signature tests cover pinned identity, forgery, cross-job replay, ciphertext substitution, JSON transport, and verify-before-decrypt ordering.
- No live provider, browser, workflow, API key, or paid service was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Result signing now has a concrete dedicated secret contract instead of relying on an arbitrary optional PEM supplied by callers.
- The signing private key is worker-only; no design requires placing it in client configuration or result metadata.
- The new loader is not yet wired into `Nvidea.Worker/Program.cs`, so deployed workers still do not automatically require this secret.
- Production client ingestion still calls legacy `ResearchResultProtector.Unprotect` and therefore does NOT yet enforce worker origin. Do not claim end-to-end worker authentication yet.
- Existing AEAD confidentiality/integrity, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- Worker process composition must call `WorkerResultSigningPrivateKeyTrust.LoadRequired()` and pass the result to `NebiusResearchWorker`; until then authenticated result production is not deployment-enforced.
- Client configuration still needs a separately pinned worker result-verification public key; ingestion still uses the legacy decrypt path.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Wire `WorkerResultSigningPrivateKeyTrust.LoadRequired()` into `Nvidea.Worker/Program.cs` and pass it to `NebiusResearchWorker`, making deployed worker result signing fail closed. Then add trusted client configuration for only the worker verification public key and migrate `RemoteResearchResultIngestor` to `AuthenticatedResearchResultProtector.Unprotect` before decryption/parsing, with actual-ingestor regressions for missing/forged signatures, wrong pinned key, cross-job replay, and ciphertext/report-receipt substitution. Retain unsigned ingestion only behind an explicit migration mode, never as authenticated authority.
