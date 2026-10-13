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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight. Actual-ingestor adversarial coverage includes unsigned results, wrong pinned worker identity, signed remote-job replay, and post-signature ciphertext/report-payload substitution. Added an operator-grade worker result-signing lifecycle runbook, recording-day signing readiness, and provider-free source-contract guards.

### 2026-10-14 — public worker authentication evidence model
Added `WorkerAuthenticationJudgeEvidence` with validated uppercase SHA-256 SPKI fingerprint and fixed RSA-PSS/SHA-256 scheme, plus provider-free privacy and malformed-input tests. This work was initially persisted on `automation/worker-auth-evidence`; `main` was not altered.

## Latest run — Windows worker-authentication judge evidence integration (2026-10-14)
Repository/branch:
- Mutation target checked before every GitHub write: exactly `UnknownGod2011/NVIDEA`.
- Code branch: `automation/worker-auth-evidence`, proposed to `main` in draft PR #1 (https://github.com/UnknownGod2011/NVIDEA/pull/1). No other repository changed.

Files changed:
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml` — added a first-class Remote worker authentication panel, adjusted grid row layout.
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs` — receives a Core public-only projection, renders it, and fails closed when missing.
- `src/Nvidea.Windows/MainWindow.Readiness.cs` — reads validated public-key fingerprint via `WorkerAuthenticationJudgeEvidence.FromPinnedEnvironment()`, catches invalid/missing configuration without exposing exception contents, passes projection to judge view.
- `tests/Nvidea.DemoPackageValidator.Tests/WorkerAuthenticationJudgeWiringTests.cs` — provider-free source/XAML contract guarding exactly one panel, production factory wiring, null fail-closed status, non-attestation wording, and absence of raw environment access in dialog.
- `progress.md` — this persistent run record.
- Prior branch files (retained): `src/Nvidea.Core/Desktop/WorkerAuthenticationJudgeEvidence.cs`, `tests/Nvidea.Core.Tests/WorkerAuthenticationJudgeEvidenceTests.cs`.

Engineering result:
- The Windows judge view shows a locally PINNED worker public-key SHA-256 fingerprint and RSA-PSS/SHA-256 scheme, or NOT VERIFIED if the projection cannot be constructed.
- A public pin is configuration readiness only: the panel explicitly says the deployed worker identity is NOT VERIFIED by this view. No cloud/worker attestation is fabricated.
- No PEM, private-key material, deployment secret name/ID/version, research prompt/source/answer, provider exception, or URL is rendered in this panel.
- Existing receipt/provenance/permissions/emergency-stop and remote authentication paths remain unchanged.

Validation/evidence:
- GitHub connector accepted four production-file commits plus the regression-test commit (latest code/test commit `9c9f43524599e6150976e9c3fc063e6a06159251`).
- Ten repository-content static checks passed: correct row count/index range, unique UI field, dialog binding, null fail-closed, no claimed deployed-worker verification, valid C# newline escape, factory + constructor wiring, no raw exception display, xUnit project present.
- `main...automation/worker-auth-evidence`: branch ahead by 6 commits before this progress update, behind by 0.
- `.NET`, PowerShell and Windows/WPF runtime are not installed here. Executable compile/test PASS is NOT claimed. New xUnit source contract is added but unexecuted.
- No Nebius/Tavily/Serverless calls, paid providers, live signed-result canary, or GitHub Actions reruns occurred.

Risks and blockers:
- Execute `dotnet test` (including Core + DemoPackageValidator projects), WPF Windows build, and interactive Judge Evidence smoke test on an appropriate Windows/.NET 8 host.
- Draft PR #1 requires review and CI/Windows qualification before merge; changes are currently branch-only.
- Public-pin presence is not proof the deployed MysteryBox signing private key corresponds to the Windows public pin.
- Single-key rotation still requires coordinated dispatch pause/drain; auth must never downgrade to unsigned results.

## Single Best Next Task
Implement a real end-to-end **signed-result canary** across deployed Nebius Worker and Windows pinned-key verifier that proves signer/pin matching without exposing research data, with negative cases for wrong key, replay and signing-version drift. Integrate its verified outcome into fail-closed recording readiness; run .NET 8 + WPF qualification and update draft PR #1 based on results.
