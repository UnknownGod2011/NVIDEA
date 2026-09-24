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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, and a dedicated worker-only signing-secret trust boundary.

## Latest run — deployed worker signing is fail-closed
Files changed:
- `src/Nvidea.Worker/Program.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, worker process composition, signing-secret trust code, and the worker-side signing implementation before changing code.
- Wired `WorkerResultSigningPrivateKeyTrust.LoadRequired()` into the real `Nvidea.Worker` process before provider clients or remote work execution are constructed.
- Passed the validated, canonicalized signing private key into `NebiusResearchWorker` as its distinct result-signing identity.
- Deployed worker composition now fails closed when `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM` is absent or invalid instead of silently publishing unsigned remote research results.
- Preserved key-role separation: `runtime.WorkerPrivateKeyPem` remains the work-item decryption identity; the new signing key is loaded independently and used only for result authentication.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms `NebiusResearchWorker` accepts the signing key as its appended sixth constructor parameter and signs the already-AEAD-protected envelope before `_results.PutAsync`.
- The signing secret loader already has provider-free regressions for dedicated-secret isolation, missing secret, public-only material, and undersized RSA identities.
- The process loads the required signing identity before creating provider HTTP clients, so secret/configuration failure occurs before any Nebius/Tavily request or paid work.
- No live provider, browser, workflow, API key, or paid service was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- A production worker process can no longer accidentally downgrade to unsigned result publication because signing-secret provisioning was omitted.
- Result signing and work-item decryption remain distinct cryptographic roles, enabling independent rotation/revocation and limiting key-purpose confusion.
- Signing happens over the protected result, so the signature authenticates encrypted payload/provenance without exposing research content.
- Production client ingestion still calls legacy `ResearchResultProtector.Unprotect` and therefore does NOT yet enforce worker origin. End-to-end worker authentication must not be claimed yet.
- Existing AEAD confidentiality/integrity, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- Deployment manifests/secret provisioning must actually supply `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM`; otherwise the worker now correctly refuses to start remote work.
- Client configuration still needs a separately pinned worker result-verification public key; ingestion still uses the legacy decrypt path.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Add a trusted client-side worker result-verification public-key boundary (public key only, never the worker signing private key), then migrate `RemoteResearchResultIngestor` to fail closed on missing/forged signatures and call `AuthenticatedResearchResultProtector.Unprotect` before any decryption/parsing. Qualify the actual ingestor boundary for missing/forged signatures, wrong pinned key, cross-job replay, and ciphertext/report-receipt substitution. Keep any legacy unsigned ingestion behind an explicit migration mode and never allow it to produce authenticated/judge authority.
