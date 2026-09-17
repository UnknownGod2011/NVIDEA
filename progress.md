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
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, process-level fault injection, and an independent receipt verifier. The independent verifier is mandatory before production PASS.

### 2026-09-17 — behavioral receipt tamper regression (latest run)
Completed:
- Re-read this ledger completely and inspected the production preflight and existing fake-dotnet behavioral harness before changing anything.
- Added `scripts/tests/preflight-receipt-tamper.behavior.ps1`.
- The harness creates an isolated copy of the real production preflight, an unmodified copy of the real receipt verifier, canonical demo manifest, deterministic fake-dotnet producer fixtures, and a test-only verifier wrapper. No production bypass/test hook was added.
- The wrapper tampers immediately after the real preflight creates its receipt and immediately before delegating to the untouched verifier.
- Behavioral cases cover receipt artifact SHA mutation, receipt artifact length mutation, invalid scope, forged provider-live flag, traversal/path substitution, and post-receipt artifact-content mutation. Every case requires a terminating failure and explicitly asserts the production PASS marker is absent.
- Added an untampered custom-artifact-directory control case requiring the real production PASS marker and receipt materialization, preserving the custom directory contract.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/tests/preflight-receipt-tamper.behavior.ps1` (new)
- `progress.md`

Validation/evidence:
- Source-level review confirms the harness delegates tampered evidence to an unmodified copy of `verify-preflight-receipt.ps1` and executes an isolated copy of `submission-preflight.ps1`.
- The harness is network/provider free and uses only local fake producer fixtures.
- No executable PowerShell/.NET PASS is claimed in this connector environment; the new behavioral harness must still be executed on the Windows/PowerShell 7 recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The test architecture avoids weakening production merely for fault injection: all tampering capability exists only in an isolated test copy/wrapper.
- Receipt verification is now behaviorally specified to fail closed for both metadata tampering and artifact mutation after receipt creation.
- Path substitution is tested alongside hash/length changes, and local receipts remain forbidden from claiming provider-live evidence.
- This remains a local consistency mechanism, not authenticity against a fully compromised machine; the receipt is intentionally unsigned.
- Provider-live evidence remains explicitly outside this zero-cost preflight and must not be inferred from its PASS.

## Known blockers / risks
- The new behavioral harness needs executable confirmation on Windows/PowerShell 7; connector-only source inspection is not execution evidence.
- Existing process harnesses likewise need a recording-machine run against the mandatory verifier gate.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Run and harden the complete zero-cost PowerShell regression suite on the actual Windows/PowerShell 7 recording environment (including the new receipt-tamper behavioral harness), fix any portability/runtime defects found, and produce a single lean `scripts/test-submission-tooling.ps1` entry point that executes all local submission contracts/behavior tests without invoking providers or GitHub Actions.
