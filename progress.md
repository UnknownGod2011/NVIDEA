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

### 2026-09-17 — network-free preflight sequencing contract (latest run)
Completed:
- Re-read this ledger completely and selected the recorded highest-value task: protect the zero-cost preflight's ordering, path confinement and provider-live exclusion from silent regression.
- Added `scripts/tests/submission-preflight.contract.ps1`, a dependency-free PowerShell source-contract regression. It does not invoke dotnet, network, providers, browser sessions or credentials.
- The contract requires validator < checklist generator < positive evaluator < adversarial evaluator ordering; requires the non-zero child-exit fail-closed guard; requires canonical repository-prefix artifact confinement; and requires the explicit provider-live skip disclosure.
- Added a narrow executable-command denylist for the unified judging verifier and accidental `NebiusLive`/`TavilyLive`/`PlaywrightLive` command fragments so the default zero-cost path cannot silently acquire provider-live execution while still allowing explanatory safety text.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/tests/submission-preflight.contract.ps1`
- `progress.md`

Validation/evidence:
- Static review against the current `scripts/submission-preflight.ps1` confirms every asserted fragment and ordering relationship exists in the production script.
- The regression itself is network-free and provider-free; it only reads the local preflight source when executed.
- Executable PowerShell validation is not claimed in this connector environment; the contract still requires execution on the Windows recording machine before submission.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The preflight continues to canonicalize artifacts and reject paths outside the repository.
- Validator failure remains upstream of checklist/evaluator execution, with child non-zero exits throwing immediately.
- The new contract makes those sequencing and confinement assumptions explicit and regression-detectable instead of relying only on review.
- Provider-live verification remains intentionally absent from the zero-cost command; fresh provider evidence must be obtained deliberately through the documented live workflow.
- The contract reads source only and cannot touch durable memory, browser profiles, provider accounts, secrets or the audit store.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this environment; the production preflight still needs an actual run on the recording machine.
- The new contract is a static source-contract test, not a process-level fault-injection test. A future stronger harness should inject a fake dotnet executable and prove at runtime that a validator exit code prevents downstream invocations.
- The unified `Nvidea.JudgingEvidenceVerifier` necessarily consumes fresh Nebius deployment PASS/model-catalog evidence and remains outside zero-cost default preflight.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Upgrade the preflight regression from source-contract assertions to a network-free process-level fault-injection harness using a temporary fake `dotnet` shim: record child invocations, force validator failure, prove generator/evaluators never execute, then exercise a success path and artifact-path escape rejection. This gives behavioral evidence for the orchestration without provider calls or GitHub Actions.
