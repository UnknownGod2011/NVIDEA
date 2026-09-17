# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-16
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, extensive crash-consistency hardening, judge-visible runtime evidence, a 168-second deterministic demo, schema-v2 per-beat execution contracts, hardened validator/regressions, and manifest/runbook alignment.

### 2026-09-17 — local submission evidence hardening
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, process-level fault injection, and an independent receipt verifier. The independent verifier is mandatory before production PASS. Added static and behavioral receipt-tamper regressions without adding a production bypass/test hook.

### 2026-09-17 — unified zero-cost regression entry point (latest run)
Completed:
- Re-read this ledger completely and enumerated the current submission regression inventory before changing anything.
- Added `scripts/test-submission-tooling.ps1` as the single lean entry point for all six local submission contracts/behavior regressions.
- Runner requires PowerShell 7+, verifies every expected test exists before execution, confines resolved test paths beneath the repository root, executes tests in deterministic order, records duration/failure summaries, fails fast by default, and supports `-ContinueOnFailure` for diagnostic recording-machine runs.
- The runner performs no provider/network/GitHub Actions operation itself; child regressions remain the existing fake/local test suite.
- It refuses to report aggregate PASS unless every expected regression executed and passed.
- Explicitly re-verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/test-submission-tooling.ps1` (new)
- `progress.md`

Validation/evidence:
- GitHub inventory confirms the runner covers the complete current six-file `scripts/tests` submission regression set: production contract, process behavior, duplicate JSON, schema shape, receipt tamper contract, and receipt tamper behavior.
- Source-level review confirms no network/provider commands are introduced by the runner.
- No executable PowerShell/.NET PASS is claimed in this connector environment; the unified entry point and its child regressions still require execution on the Windows/PowerShell 7 recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The unified runner is intentionally orchestration-only and does not weaken production verification or introduce test hooks.
- Test discovery is explicit rather than wildcard-based, preventing an unexpected newly dropped script from silently joining the trusted suite.
- Resolved test paths must remain beneath the repository root before invocation.
- Fail-fast is the safe default; diagnostic continuation is explicit and aggregate PASS remains impossible if any test fails.
- Receipt verification remains a local consistency mechanism, not authenticity against a fully compromised machine; provider-live evidence remains outside this zero-cost suite.

## Known blockers / risks
- The complete unified suite needs executable confirmation on Windows/PowerShell 7; connector-only source inspection is not execution evidence.
- Any portability/runtime defect discovered there must be fixed before relying on the suite for recording-day confidence.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Execute `scripts/test-submission-tooling.ps1 -ContinueOnFailure` on the actual Windows/PowerShell 7 recording environment, fix every portability/runtime failure found, then run it again fail-fast and preserve the real PASS output as local recording-day evidence. After that, return focus to live end-to-end judge/demo validation rather than adding more zero-cost harness layers unless a concrete defect is discovered.
