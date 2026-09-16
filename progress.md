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

### 2026-09-17 — zero-cost submission preflight orchestrator (latest run)
Completed:
- Re-read this ledger completely and selected the recorded highest-value task: create one fail-closed local preflight command that cannot refresh the recording checklist before the canonical manifest passes validation.
- Added `scripts/submission-preflight.ps1` for the Windows recording machine. It verifies the repository/manifest and .NET SDK locally, confines generated artifacts to the repository, runs `Nvidea.DemoPackageValidator` first, generates the checklist only after validator PASS, then runs the positive and adversarial Personal AI evaluators.
- Provider-live work is deliberately excluded from the default command. The script explicitly states that it does not invoke Nebius catalog/live PASS, Tavily, browser, inference, or the unified judging verifier because those require fresh provider/live evidence and must not be silently represented by a zero-cost preflight.
- The orchestrator fails immediately on every non-zero child exit and uses the validator/checklist/evaluator programs' existing atomic outputs; no stale checklist is regenerated after validation failure.
- During final review, caught and corrected the checklist generator invocation to its actual `--output <path>` CLI contract before ending the run; removed an unused provider-live switch rather than exposing a flag that implied live verification it did not perform.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `progress.md`

Validation/evidence:
- Static contract review against `Nvidea.DemoPackageValidator`, `Nvidea.DemoChecklistGenerator`, `docs/demo-package.json`, and the evaluator project paths confirms the orchestrator uses the current CLI/project contracts and preserves validator-before-generator ordering.
- The script itself contains no provider credentials and performs no network/provider/browser operation by design; child positive/adversarial evaluators are the existing deterministic local checks.
- Executable validation remains unavailable in this environment: no usable `dotnet`, `csc` or `msbuild` is available here, so no PowerShell/.NET execution PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Artifacts must resolve beneath the selected repository root; an outside absolute/relative artifact directory is rejected.
- Manifest validation is the first product command and gates checklist generation; malformed/unsafe schema-v2 content cannot refresh the operator checklist through this workflow.
- Any validator/generator/evaluator non-zero exit aborts the preflight; there is no best-effort continuation that could print a misleading PASS.
- Provider-live verification is intentionally not automated by this zero-cost command, preventing accidental paid calls or stale/captured evidence from being treated as current live proof.
- The orchestrator does not touch durable memory, browser profiles, provider accounts, secrets, or the audit store.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; the orchestrator and current generator are statically reviewed but unexecuted.
- The final Windows machine still needs an actual execution of `scripts/submission-preflight.ps1` before recording.
- The unified `Nvidea.JudgingEvidenceVerifier` necessarily consumes fresh Nebius deployment PASS/model-catalog evidence and is therefore not part of zero-cost default preflight; a separate deliberate live-evidence step remains required when making provider-live claims.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add a small network-free contract test around `scripts/submission-preflight.ps1` (or factor its sequencing into a testable .NET orchestrator) that proves validator failure prevents checklist/evaluator execution, verifies artifact-path confinement, and proves provider-live commands cannot be reached by the default path. Then run the complete zero-cost preflight on the first available .NET 8 Windows environment before recording.
