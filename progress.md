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
Upgraded `docs/demo-package.json` and `Nvidea.DemoPackageValidator` to schema v2. Every judging beat declares expected production session milestones, preflight dependencies and a fail-closed fallback policy; the validator enforces closed milestone mappings, duration/path/evidence/provider-live/command constraints and strict JSON shape.

### 2026-09-16 — restored validator security defenses (latest run)
Completed:
- Re-read this ledger completely and inspected the current schema-v2 validator before mutation.
- Retrieved the last hardened v1 validator from repository history and restored its two accidentally dropped submission defenses without weakening v2 per-beat contracts.
- Restored the explicit required-repository-asset allowlist/check for README, LICENSE, evaluator documentation and the three evaluator/verifier project files.
- Restored the heuristic manifest secret scan for private-key headers, bearer authorization material, API-key assignments and common `sk-` secret prefixes.
- Extended the secret scan to schema-v2 strings, including `fallbackPolicy`, `preflightDependencies`, and `expectedSessionMilestones`, in addition to title, claims, paths, evidence and commands.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every successful GitHub mutation; no other repository was mutated.

Files changed this run:
- `tools/Nvidea.DemoPackageValidator/Program.cs`
- `progress.md`

Validation/evidence:
- Repository history commit `52c30d65...` supplied the exact prior required-asset list and secret-detection rules, avoiding reconstruction from memory.
- Static inspection confirms both restored checks coexist with schema-v2 milestone/preflight/fallback validation.
- Secret enumeration now includes all newly introduced free-text v2 fields so moving a credential into a fallback or preflight description does not evade the heuristic scan.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation or validator PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Demo fallbacks remain fail-closed rather than bypassing login/CAPTCHA/MFA, exact-scope approval, browser verification, or evidence provenance.
- Required repository assets can no longer silently disappear while referenced demo paths still happen to validate.
- Manifest free text is again screened for obvious committed credentials, including the new v2 execution-contract fields.
- The secret scan is intentionally heuristic defense-in-depth, not a substitute for repository-wide secret scanning or provider-side secret management.
- Milestone vocabulary remains closed and session proof remains payload-free/process-local.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Add executable fail-closed fixtures/regressions for missing/wrong milestone, missing preflight, empty fallback, duplicate properties, duration overflow, missing required assets and secret-bearing v2 fields once a .NET environment is available.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Final recording still requires a real Windows demo-machine preflight.

## Single Best Next Task
Add a network-free negative-fixture regression harness for `Nvidea.DemoPackageValidator` covering the schema-v2 execution contracts plus the restored required-asset and secret-scan defenses, so submission integrity is executable rather than static-only.