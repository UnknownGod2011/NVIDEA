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
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, process-level fault injection, and an independent receipt verifier. The independent verifier is mandatory before production PASS. Added static and behavioral receipt-tamper regressions without adding a production bypass/test hook. Added `scripts/test-submission-tooling.ps1` as the deterministic single entry point for all six zero-cost submission regressions.

### 2026-09-17 — recording-day evidence capture (latest run)
Completed:
- Re-read this ledger completely, inspected current repository state/recent commits, and kept the next priority focused on obtaining trustworthy executable recording-machine evidence rather than adding another independent harness.
- Extended `scripts/test-submission-tooling.ps1` with optional `-EvidencePath` output so a real Windows/PowerShell 7 run can preserve machine-readable evidence instead of relying on transient console text.
- Evidence is repository-confined, JSON-only, written atomically, and records UTC timing, PowerShell/runtime OS/architecture, expected/executed test counts, per-test duration/failure state, continuation mode, and aggregate PASS.
- The evidence explicitly declares `scope=local-zero-cost` and `providerLiveEvidence=false`; it cannot legitimately be presented as Nebius/Tavily/browser live evidence.
- Aggregate evidence PASS requires zero failed tests AND execution of all six expected regressions. A fail-fast run that stops early is persisted as `overallPassed=false`.
- Re-verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/test-submission-tooling.ps1`
- `progress.md`

Validation/evidence:
- Source-level review confirms evidence paths are canonicalized beneath repository root and require `.json`; temporary writes are atomically moved into place and cleaned in `finally`.
- Evidence captures no child stdout, credentials, provider payloads, prompts, browser data, or secrets; only test names, status, duration and exception message are persisted.
- No executable Windows/PowerShell 7 PASS is claimed in this connector environment. The new evidence path exists specifically so the actual recording machine can preserve that proof.
- Fresh official-doc web searches for Nebius/Nemotron model assumptions returned no usable official search result this run, so no model/API claim or configuration was changed on guesswork.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The unified runner remains orchestration-only and does not weaken production verification or introduce test hooks.
- Evidence output cannot escape the repository, is opt-in, and explicitly distinguishes local regression evidence from live-provider evidence.
- Fail-fast remains the safe default; diagnostic continuation is explicit. Both modes can persist evidence, but aggregate PASS is impossible unless every expected regression executed and passed.
- The evidence file is diagnostic provenance, not a cryptographic attestation against a compromised local machine. Production receipt verification remains separate.
- Test discovery remains explicit rather than wildcard-based, preventing an unexpected newly dropped script from silently joining the trusted suite.

## Known blockers / risks
- The complete unified suite still needs executable confirmation on Windows/PowerShell 7; connector-only source inspection is not execution evidence.
- Any portability/runtime defect discovered there must be fixed before relying on the suite for recording-day confidence.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
On the actual Windows recording machine run `pwsh -File scripts/test-submission-tooling.ps1 -ContinueOnFailure -EvidencePath artifacts/submission-tooling/windows-diagnostic.json`, fix every failure, then run fail-fast with `-EvidencePath artifacts/submission-tooling/windows-pass.json`. Only after `overallPassed=true` is genuinely produced should zero-cost harness work stop; then immediately prioritize live end-to-end judge/demo validation across Nebius/Nemotron, Tavily, authenticated Playwright, Windows UX, permission gates, durable memory and background execution.
