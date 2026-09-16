# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-15
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, and extensive crash-consistency hardening for remote dispatch, binding, result ingestion, cleanup and recovery.

### 2026-09-16 — judge-visible runtime evidence
Added `JudgeEvidenceDialog` plus `SessionEvidenceLedger`, with production observation for successful Nemotron inference, memory influence, validated Tavily citations, trusted browser post-state verification, exact-scope consequential approval, and authenticated/audited Nebius background-result application. Added adversarial contracts around browser and remote evidence boundaries and a safe `New demo session` reset that clears only ephemeral session proof.

### 2026-09-16 — deterministic judge demo
Added a 168-second operator runbook aligned to the machine-readable demo package, with strict preflight/post-take rejection gates and explicit separation of synthetic, local-live, provider-live and documentation evidence.

### 2026-09-16 — executable per-beat demo contracts
Upgraded `docs/demo-package.json` and `Nvidea.DemoPackageValidator` to schema v2. Every judging beat declares expected production session milestones, preflight dependencies and a fail-closed fallback policy; the validator enforces closed milestone mappings, duration/path/evidence/provider-live/command constraints and strict JSON shape. Restored the explicit required-repository-asset allowlist and heuristic secret scan, including all schema-v2 free-text fields.

### 2026-09-16 — schema-v2 validator regression harness
Migrated validator fixtures to schema v2 and added network-free fail-closed regressions for missing preflights/fallbacks, wrong or missing milestones, architecture milestone pollution, duration overflow, missing mandatory assets, duplicate JSON properties, path traversal, provider-live mismatch and secrets in schema-v2 fields.

### 2026-09-16 — submission-readiness manifest/runbook drift audit
Aligned the operator runbook to the exact production milestone vocabulary and made Tavily/Nebius/browser/approval fallback semantics explicitly fail-closed. Canonical live sequence: `NemotronInferenceCompleted`, `MemoryInfluencedResponse`, `TavilyValidatedCitationUsed`, `BrowserVerifiedGoalCompleted`, `ConsequentialApprovalGranted`, `NebiusBackgroundExecutionObserved`; architecture has no expected runtime milestone.

### 2026-09-17 — manifest-driven operator checklist generator
Added `tools/Nvidea.DemoChecklistGenerator`, a dependency-free .NET 8 CLI that derives the recording checklist directly from schema-v2 beat order, durations, judge claims, preflight dependencies, exact expected session milestones and fail-closed fallback policies. Parsing is strict and bounded; output is atomic.

### 2026-09-17 — zero-cost submission preflight orchestrator
Added `scripts/submission-preflight.ps1`: repository/manifest/.NET checks, repository-confined artifacts, validator first, checklist generation only after validation, then deterministic positive and adversarial evaluators. Provider-live work is deliberately excluded and disclosed.

### 2026-09-17 — network-free preflight sequencing contract
Added `scripts/tests/submission-preflight.contract.ps1`, a source-contract regression enforcing validator → checklist → positive → adversarial ordering, non-zero fail-closed handling, repository-confined artifacts, and absence of provider-live executable commands.

### 2026-09-17 — process-level preflight fault injection (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value task rather than adding another documentation-only check.
- Added `scripts/tests/submission-preflight.behavior.ps1`, a Windows PowerShell process-level regression that temporarily shadows `dotnet` with a local `dotnet.cmd` shim. The shim records child invocations and performs no network, inference, browser, provider, credential or GitHub operation.
- Validator-failure scenario injects exit code 41 for `Nvidea.DemoPackageValidator.csproj` and requires the production preflight to throw immediately with exactly one recorded child call; checklist generation and both evaluators therefore cannot execute.
- Success scenario requires exactly four child invocations in canonical order: validator, checklist generator, positive evaluator, adversarial evaluator. It also rejects any observed judging-verifier/NebiusLive/TavilyLive/PlaywrightLive command fragment.
- Repository-confinement scenario supplies an absolute artifact directory outside NVIDEA and requires rejection before any child process executes.
- The harness restores PATH/test environment variables and removes all temporary/failure/success test artifacts in `finally`, including when an assertion fails.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/tests/submission-preflight.behavior.ps1`
- `progress.md`

Validation/evidence:
- Static review confirms the harness targets the current production labels/projects/error messages and exercises the actual `scripts/submission-preflight.ps1`, rather than a copied orchestration implementation.
- The fake-dotnet path is intentionally Windows-specific because the submission preflight and final recording target Windows; it is provider/network free by construction.
- Executable PowerShell validation is not claimed in this connector environment. The harness must still be run on the Windows recording machine; no fabricated PASS is recorded.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Fault injection proves the intended behavioral boundary: validator non-zero exit is upstream of all manifest-derived or evaluator work.
- Artifact escape rejection is tested before child execution, reducing the chance that preflight writes evidence outside the repository.
- Provider-live commands are absent from the observed success sequence and remain deliberately separate from zero-cost preflight.
- The shim receives no secrets and records only command-line arguments; the production zero-cost commands are not expected to contain credentials.
- Temporary PATH/environment mutation is process-scoped and restored in `finally`; temporary files and test artifact directories are cleaned on both success and failure.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; production preflight plus both PowerShell regressions still need execution on the actual Windows recording machine.
- The fake shim returns success without producing the output files named by `--output`. Today the production preflight trusts a zero child exit and does not independently require each expected artifact to exist/non-empty. A broken or malicious child could therefore return zero without creating evidence.
- The unified `Nvidea.JudgingEvidenceVerifier` necessarily consumes fresh Nebius deployment PASS/model-catalog evidence and remains outside zero-cost default preflight.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Harden `scripts/submission-preflight.ps1` so every zero-cost child must both exit zero and materialize its expected repository-confined output file (preferably non-empty) before the next stage runs. Update the fake-dotnet behavior harness to create deterministic placeholder outputs on success and add a fault mode that exits zero while withholding an artifact, proving the preflight fails closed against false-success child processes.
