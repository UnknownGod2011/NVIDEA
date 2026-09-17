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
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, process-level fault injection, and an independent receipt verifier.

### 2026-09-17 — mandatory independent receipt gate (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value task.
- Wired `scripts/verify-preflight-receipt.ps1` into `scripts/submission-preflight.ps1` after receipt materialization and before every production PASS message.
- Production PASS therefore now depends on independent receipt verification; verifier exceptions fail the parent preflight under `$ErrorActionPreference = Stop`.
- Added an explicit startup requirement that the independent verifier itself exists before any child evaluator work begins.
- Preserved custom confined `ArtifactsDirectory`, zero-cost/provider-live exclusion, manifest binding, semantic PASS checks and artifact hashing.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `progress.md`

Validation/evidence:
- Static control-flow review confirms the independent verifier executes after receipt creation and before the two PASS/status lines.
- A verifier throw terminates the preflight because terminating errors are enabled; therefore a rejected/tampered receipt cannot proceed to PASS through the normal production path.
- No executable PowerShell/.NET validation is claimed in this connector environment; run the preflight and regression suite on the Windows recording machine before relying on the gate.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Local PASS now requires two layers: producer/evidence semantic checks followed by independent receipt/artifact consistency verification.
- Receipt verification re-resolves and rehashes bound artifacts and rejects provider-live claims, path substitution/traversal, malformed metadata and manifest mismatch.
- This remains a local consistency mechanism, not authenticity against a fully compromised machine; the receipt is intentionally unsigned.
- Provider-live evidence remains explicitly outside this zero-cost preflight and must not be inferred from its PASS.

## Known blockers / risks
- A dedicated production-path fault-injection regression still needs to tamper the receipt/artifacts immediately before independent verification and prove the PASS marker is unreachable.
- The existing process harnesses should be checked for compatibility with the newly mandatory verifier invocation.
- No executable .NET/Windows validation has been performed in this connector environment.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add a network-free production-path receipt fault-injection regression that runs the actual preflight, intercepts/tampers the freshly generated receipt or bound artifact immediately before verification, and asserts the process cannot emit `Submission preflight PASS`; cover hash, length, path/scope/provider flag and artifact-content tampering while preserving cleanup and custom-artifact-directory behavior.
