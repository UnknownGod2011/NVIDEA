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

### 2026-09-17 — manifest-driven operator checklist generator (latest run)
Completed:
- Re-read this ledger completely and selected the recorded highest-value task: eliminate future manifest/runbook drift by deriving the recording checklist from schema v2 rather than maintaining another hand-written copy.
- Added `tools/Nvidea.DemoChecklistGenerator`, a dependency-free .NET 8 CLI that reads `docs/demo-package.json` and emits a concise Markdown recording checklist directly from beat order, duration, judge claim, preflight dependencies, exact expected session milestones and fail-closed fallback policies.
- The generated checklist includes the total 168-second plan, 180-second maximum, computed contingency, per-beat checkboxes, explicit no-runtime-milestone handling for the architecture close, and a final take-acceptance gate forbidding synthetic/documentation evidence from being relabeled as live proof.
- Generator parsing is fail-closed: schema v2 only, strict unmapped-member rejection, duplicate-property rejection, bounded manifest size/depth/beat count, unique beat IDs, non-empty execution contracts, bounded milestone lists, checked duration accumulation and atomic output writes.
- During review, caught an initial strict-deserialization incompatibility because the first record shape omitted existing manifest fields. Corrected it before ending the run by modeling the complete schema-v2 top-level/beat/evidence/command shape; the final committed generator no longer rejects valid fields as unmapped.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `tools/Nvidea.DemoChecklistGenerator/Nvidea.DemoChecklistGenerator.csproj`
- `tools/Nvidea.DemoChecklistGenerator/Program.cs`
- `progress.md`

Validation/evidence:
- Static review against the actual `docs/demo-package.json` confirms the generator record shape covers all current schema-v2 properties and renders the exact manifest milestone/preflight/fallback values rather than maintaining a second milestone mapping.
- The generator performs no network/provider/browser operations and requires no credentials.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation or runtime PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The generator is read-only with respect to the manifest and writes only the explicitly requested checklist output through an atomic temp-file replacement.
- It copies operator-facing manifest text into Markdown, so it intentionally does not claim to sanitize an already-compromised manifest; the hardened `Nvidea.DemoPackageValidator` remains the security/contract gate and should run before checklist generation in the final workflow.
- Duplicate JSON properties, unknown schema fields, malformed execution contracts and duration overflow fail generation rather than producing a misleading partial checklist.
- No product authority, durable memory, browser session, provider account, secret or audit store is touched.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current generator is statically reviewed but unexecuted.
- The generator is not yet wired into a checked-in one-command submission/preflight workflow, so an operator could still accidentally use an old generated checklist.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Final recording still requires a real Windows demo-machine preflight.
- The schema-v2 validator and generator should both be executed on a .NET 8 machine before submission.

## Single Best Next Task
Add a small submission-preflight orchestrator that first runs `Nvidea.DemoPackageValidator` and only on PASS generates the operator checklist, then runs the existing positive/adversarial/judging evidence checks without triggering paid/live provider operations by default. This creates one fail-closed, zero-cost command for the final Windows recording preflight and prevents a stale checklist from bypassing manifest validation.
