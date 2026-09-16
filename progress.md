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

### 2026-09-16 — schema-v2 validator regression harness (latest run)
Completed:
- Re-read this ledger completely and inspected the current validator plus its existing test project before mutation.
- Found that `DemoPackageValidatorTests.cs` still generated schema-v1 fixtures, meaning its nominal positive test could no longer exercise the current schema-v2 validator successfully.
- Migrated the fixture to schema v2 with the exact seven closed beat IDs and production milestone mappings.
- Added network-free fail-closed regressions for missing preflights, blank fallback policies, wrong/missing milestones, architecture-proof milestone pollution, duration overflow, missing required assets, duplicate JSON properties, path traversal, provider-live claim mismatch, and secret material placed in schema-v2 fallback/preflight/milestone fields.
- Kept tests isolated in a temporary repository-shaped fixture; they require no network, provider credentials, browser session, or cloud account.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every successful GitHub mutation; no other repository was mutated.

Files changed this run:
- `tests/Nvidea.DemoPackageValidator.Tests/DemoPackageValidatorTests.cs`
- `progress.md`

Validation/evidence:
- Static comparison against `tools/Nvidea.DemoPackageValidator/Program.cs` confirms fixture schema version, beat IDs, expected milestone vocabulary, required assets and tested failure surfaces match the current validator contract.
- The duplicate-property test writes raw JSON rather than reserializing a dictionary, so it actually exercises the validator's duplicate JSON detection.
- The v2 secret tests place credential markers specifically in newly introduced execution-contract fields.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation or xUnit PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Demo fallbacks remain fail-closed rather than bypassing login/CAPTCHA/MFA, exact-scope approval, browser verification, or evidence provenance.
- Submission integrity defenses now have explicit negative contracts rather than relying only on static review.
- Test fixtures contain synthetic marker strings only and never use real credentials or provider endpoints.
- Milestone vocabulary remains closed and session proof remains payload-free/process-local.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Final recording still requires a real Windows demo-machine preflight.
- The validator test project should be executed on a .NET 8 machine before submission to catch compile/runtime drift that static review cannot prove.

## Single Best Next Task
Perform a submission-readiness audit of the actual `docs/demo-package.json` against the now-hardened schema-v2 validator and operator runbook, then fix any drift in claims, evidence paths, provider-live classification, preflights, fallbacks, or command paths before final Windows live preflight.