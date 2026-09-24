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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight. Actual-ingestor adversarial coverage includes unsigned results, wrong pinned worker identity, signed remote-job replay, and post-signature ciphertext/report-payload substitution.

## Latest run — worker result-signing identity lifecycle
Files changed:
- `docs/worker-result-signing-key-lifecycle.md`
- `progress.md`

Completed:
- Re-read `progress.md` completely and inspected the existing Nebius research-worker deployment/runbook before changing anything.
- Added an operator-grade lifecycle runbook for the dedicated worker result-signing identity, explicitly separate from work-item decryption and both client keypairs.
- Documented safe RSA-3072 generation, public fingerprinting, private-key handling, version-pinned MysteryBox provisioning, public-only Windows pin distribution, and the fail-closed configuration names used by production composition.
- Defined a coordinated single-pin rotation protocol that drains old in-flight jobs before switching worker secret version and client pin, rather than weakening authentication or accepting two identities implicitly.
- Defined symmetric rollback and a distinct compromise/revocation procedure. A compromised signing key is explicitly forbidden as a rollback target; affected research must be rerun after a fresh identity is established.
- Added a release checklist covering role uniqueness, plaintext-secret exclusion, immutable secret-version pinning, out-of-band public fingerprint verification, canary validation, and rollback readiness.
- Kept provider commands deliberately non-fabricated: current Nebius control-plane syntax was not discoverable from official web search in this environment, so the runbook specifies NVIDEA's invariant contract and requires current official MysteryBox instructions for the actual account operation.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before each mutation.

Validation/evidence:
- Static inspection of `docs/nebius-research-worker.md` confirms the existing three-role cryptographic separation, MysteryBox-only private worker provisioning, public-only client identities, and live deployment preflight contract; the new runbook extends that operational model to the fourth dedicated worker result-signing identity.
- Existing production code/tests already fail closed on missing/malformed signing secrets and verification pins and qualify unsigned, wrong-key, replay, and ciphertext substitution before decryption; this run did not claim new executable test coverage.
- No private key, API key, MysteryBox secret, provider account, browser, workflow, paid service, or other repository was accessed or mutated.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- The lifecycle avoids secret-role reuse and plaintext private-key deployment.
- Single-pin rotation explicitly prevents an unaudited multi-key grace period and avoids accepting unsigned results during maintenance.
- Compromise response distinguishes revocation from ordinary rollback and treats post-compromise results as untrusted even when cryptographically valid under the revoked key.
- Public-key fingerprints are treated only as operator verification metadata; the actual PEM pin remains the runtime trust root.
- Existing AEAD confidentiality, verify-before-decrypt worker authentication, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The actual Nebius account still needs a real MysteryBox secret/version containing the worker result-signing private PEM plus a matching client public verification pin; no secret was created or read in this run.
- Rotation currently requires a coordinated dispatch maintenance window because the verifier intentionally pins one worker identity; a multi-key grace ring is not implemented and must not be assumed.
- Exact current Nebius MysteryBox UI/CLI provisioning syntax still needs live official-account verification; documentation deliberately avoids invented commands.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Implement and qualify a first-class signing-identity readiness projection for the Windows/live-demo surface: expose only non-secret public-key fingerprint/version-reference readiness, prove private material can never enter judge evidence/logging, and make the recording gate fail closed when worker-signing deployment or client verification-pin readiness is absent or inconsistent.
