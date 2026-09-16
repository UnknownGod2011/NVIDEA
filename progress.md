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

### 2026-09-17 — process-level preflight fault injection
Added `scripts/tests/submission-preflight.behavior.ps1`, a Windows fake-`dotnet` process regression proving validator non-zero failure blocks downstream children, the successful child sequence is exactly validator → checklist → positive → adversarial, provider-live commands remain absent, and artifact-directory escape is rejected before child execution.

### 2026-09-17 — fresh artifact materialization hardening (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value reliability task.
- Hardened `scripts/submission-preflight.ps1`: every zero-cost child now has an explicit expected output path; that path is independently normalized and required to remain beneath the canonical repository root.
- Before each child launch, any prior output at that path is deleted so stale evidence cannot satisfy a new run.
- A zero child exit is no longer sufficient. Preflight now requires the expected output to exist as a file and have non-zero length before the next stage can execute; missing or empty output fails closed.
- Extended `scripts/tests/submission-preflight.behavior.ps1` fake-dotnet shim to parse `--output` and materialize deterministic non-empty placeholders during successful scenarios.
- Added a false-success fault mode: the validator returns exit 0 while deliberately withholding its artifact. The behavior contract requires preflight failure after exactly one child invocation, proving checklist/evaluator execution cannot proceed on a lying/broken child.
- Success behavior additionally asserts all four expected fresh artifacts exist and are non-empty.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `scripts/tests/submission-preflight.behavior.ps1`
- `progress.md`

Validation/evidence:
- Static review confirms all four production child calls pass their exact `--output` path into the new materialization gate and that stale outputs are removed before execution.
- Behavior harness exercises the actual production preflight and now distinguishes non-zero child failure from zero-exit/missing-artifact false success.
- Executable PowerShell/.NET validation is not claimed in this connector environment. The behavior/contract scripts still need execution on the Windows recording machine; no fabricated PASS is recorded.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- A child process can no longer advance the judging pipeline solely by returning exit 0; it must create fresh non-empty repository-confined output.
- Deleting the expected file before launch prevents a stale artifact from a previous successful run from masking a current child failure.
- Repository-prefix checking is repeated at the expected-output boundary, preserving defense in depth if future callers construct output paths differently.
- Provider-live commands remain outside the zero-cost workflow; no credential or network behavior was added.
- The fake shim receives no secrets and writes only deterministic placeholders to repository-confined test artifact directories, which the harness cleans in `finally`.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; production preflight plus PowerShell regressions still need execution on the actual Windows recording machine.
- Non-empty output is stronger than exit-code-only validation but does not prove semantic validity of each generated JSON/Markdown artifact. The validator/evaluators own their content contracts; preflight currently does not independently parse their output documents after generation.
- The unified `Nvidea.JudgingEvidenceVerifier` necessarily consumes fresh Nebius deployment PASS/model-catalog evidence and remains outside zero-cost default preflight.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add a zero-cost semantic artifact verification layer after each child: parse validator/positive/adversarial JSON with strict bounded JSON handling and require an explicit successful result/schema marker, plus structurally sanity-check the generated checklist against the canonical manifest beat count/order. Extend the fake-dotnet harness with malformed/non-PASS artifacts that are non-empty, proving preflight cannot be fooled by syntactically present but semantically invalid evidence.
