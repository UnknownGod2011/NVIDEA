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

### 2026-09-16 — executable per-beat demo contracts (latest run)
Completed:
- Re-read this ledger completely and inspected `docs/demo-package.json` and `tools/Nvidea.DemoPackageValidator/Program.cs` before mutation.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every successful GitHub mutation; no other repository was mutated.
- Upgraded `docs/demo-package.json` to schema v2. Every judging beat now declares `expectedSessionMilestones`, `preflightDependencies`, and a fail-closed `fallbackPolicy`.
- Bound the six live product beats to the exact production evidence enum names: `NemotronInferenceCompleted`, `MemoryInfluencedResponse`, `TavilyValidatedCitationUsed`, `BrowserVerifiedGoalCompleted`, `ConsequentialApprovalGranted`, and `NebiusBackgroundExecutionObserved`; architecture close intentionally expects no session milestone.
- Upgraded `Nvidea.DemoPackageValidator` to schema v2 and added closed per-beat milestone contracts plus mandatory preflight/fallback validation. Unknown milestone names, missing dependencies, missing fallback policy, duplicate/wrong milestone mappings, unsafe paths, missing referenced paths, invalid evidence classes, unsafe provider-live claims, invalid commands, duplicate JSON properties, oversize/deep manifests, and >180-second plans fail validation.
- Kept validation network-free and bounded; the validator does not execute provider calls or browser actions.

Files changed this run:
- `docs/demo-package.json`
- `tools/Nvidea.DemoPackageValidator/Program.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms the v2 manifest retains all seven required beats and the same 168-second budget.
- Strict JSON deserialization now requires the new contract fields, and the validator compares each required beat against a closed expected milestone mapping rather than accepting arbitrary strings.
- Repository/path/evidence/provider-live/command validation remains present in the rewritten validator.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation or validator PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Demo fallbacks explicitly fail closed instead of bypassing login/CAPTCHA/MFA, exact-scope approval, browser verification, or evidence provenance.
- The Nebius beat forbids relabeling dispatched-only, captured, synthetic or stale evidence as authenticated live background execution.
- The Tavily beat forbids substituting synthetic evaluator output for a missing validated live citation.
- Milestone vocabulary is closed in the validator, reducing documentation drift and typo-based false judging claims.
- Session proof remains payload-free and process-local; the manifest stores only milestone names, preflight descriptions and fallback policy text.
- Regression note: while compacting the validator for schema v2, the previous heuristic manifest secret-string scan and explicit required-repository-asset allowlist were not carried forward. Referenced-path validation remains, but this is a real validation regression and must be restored before treating v2 as submission-ready.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Restore the previous secret-string scan and explicit required-repository-asset checks in the v2 validator; do not ship the compact validator without them.
- Add executable fail-closed fixtures/regressions for missing/wrong milestone, missing preflight, empty fallback, duplicate properties and duration overflow once a .NET environment is available.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Final recording still requires a real Windows demo-machine preflight.

## Single Best Next Task
Restore the v1 validator's secret-string scan and explicit required-repository-asset checks into the schema-v2 validator without weakening the new per-beat contracts, then add negative fixture regressions proving malformed or drifted judging manifests fail closed.