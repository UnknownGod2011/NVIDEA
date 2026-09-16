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
Migrated `DemoPackageValidatorTests.cs` fixtures to schema v2 and added network-free fail-closed regressions for missing preflights/fallbacks, wrong or missing milestones, architecture milestone pollution, duration overflow, missing mandatory assets, duplicate JSON properties, path traversal, provider-live mismatch and secrets in schema-v2 fields.

### 2026-09-16 — submission-readiness manifest/runbook drift audit (latest run)
Completed:
- Re-read this ledger completely, then audited the actual `docs/demo-package.json` against `docs/judge-demo-runbook.md` rather than relying on the validator fixture alone.
- Found three operator-runbook milestone names that had drifted from the schema-v2 manifest/production closed vocabulary: `TavilyResearchWithValidatedCitation`, `BrowserPostStateVerified`, and `ConsequentialApprovalGateExercised`.
- Replaced them with the exact production/schema-v2 values `TavilyValidatedCitationUsed`, `BrowserVerifiedGoalCompleted`, and `ConsequentialApprovalGranted`.
- Tightened the Tavily and Nebius fallbacks so missing live evidence explicitly fails the corresponding live beat. Synthetic/documentation evidence may still be shown as its weaker evidence class but can no longer be read as satisfying an expected live session milestone.
- Added the exact six-milestone expected live sequence to the post-take gate and clarified that the architecture beat intentionally has no runtime milestone.
- Clarified browser proof semantics: the goal-completion milestone requires trusted verified completion, while approval evidence proves exact-scope approval only and does not itself prove the subsequent mutation succeeded.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation; no other repository was mutated.

Files changed this run:
- `docs/judge-demo-runbook.md`
- `progress.md`

Validation/evidence:
- Static cross-check against the actual schema-v2 manifest confirms the runbook now uses the exact milestone sequence: `NemotronInferenceCompleted`, `MemoryInfluencedResponse`, `TavilyValidatedCitationUsed`, `BrowserVerifiedGoalCompleted`, `ConsequentialApprovalGranted`, `NebiusBackgroundExecutionObserved`; architecture has no expected milestone.
- The timing remains unchanged at 168 seconds plus the existing 12-second contingency within the 180-second cap.
- No manifest/provider/API assumptions changed this run, so no external SDK documentation was needed.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation, validator execution or xUnit PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Demo fallbacks are now uniformly fail-closed: missing live Tavily, browser verification, exact-scope approval or authenticated/audited Nebius evidence cannot be upgraded into a successful live beat by narration.
- The runbook continues to prohibit login/CAPTCHA/MFA bypass, manual completion presented as agent success, credential display, and synthetic evidence presented as provider-live proof.
- Session evidence remains payload-free/process-local and the architecture close carries no fabricated runtime milestone.
- No product authority, durable store, browser session, provider account or secret was mutated by this documentation-only alignment.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Final recording still requires a real Windows demo-machine preflight.
- The schema-v2 validator and regression project should be executed on a .NET 8 machine before submission to catch compile/runtime drift that static review cannot prove.
- The actual manifest and runbook are now aligned on milestone names, but this equivalence is not yet mechanically enforced; future documentation edits could drift again.

## Single Best Next Task
Make manifest/runbook drift mechanically detectable without parsing prose heuristically: generate a concise operator checklist from `docs/demo-package.json` (or add a validator-produced checklist artifact) containing beat order, duration, exact expected milestones, preflights and fail-closed fallbacks, then use that generated artifact as the recording checklist so schema-v2 remains the single source of truth.