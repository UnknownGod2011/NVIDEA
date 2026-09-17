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

### 2026-09-17 — receipt tamper contract regression (latest run)
Completed:
- Re-read this ledger completely and inspected the current production preflight plus existing process-level fake-dotnet harness before changing anything.
- Added `scripts/tests/preflight-receipt-tamper.contract.ps1`, a network/provider-free regression protecting the production verifier-to-PASS control-flow boundary.
- The contract requires the independent receipt verifier invocation to remain strictly before the PASS marker and rejects a catch-and-continue region that could suppress verifier failure.
- It protects terminating-error semantics and rejects production preflight parameters that would expose obvious receipt-verification bypasses (`SkipReceipt`, `SkipVerification`, `DisableVerification`, `TrustReceipt`, `ForcePass`).
- It also pins the verifier's critical tamper-defense primitives: independent file hashing/metadata reads, path canonicalization, artifact/manifest SHA and length bindings, local-zero-cost scope and explicit provider-live denial.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/tests/preflight-receipt-tamper.contract.ps1` (new)
- `progress.md`

Validation/evidence:
- Source inspection confirms production verification currently precedes PASS and uses `$ErrorActionPreference = Stop`.
- The new regression is intentionally static/network-free and does NOT yet claim the stronger process-level tamper injection requested below.
- No executable PowerShell/.NET PASS is claimed in this connector environment; run all PowerShell regressions on the Windows recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Local PASS requires producer/evidence semantic checks followed by independent receipt/artifact consistency verification.
- Receipt verification re-resolves and rehashes bound artifacts and rejects provider-live claims, path substitution/traversal, malformed metadata and manifest mismatch.
- The new contract reduces regression risk that a future refactor silently moves, bypasses or suppresses this verifier gate.
- This remains a local consistency mechanism, not authenticity against a fully compromised machine; the receipt is intentionally unsigned.
- Provider-live evidence remains explicitly outside this zero-cost preflight and must not be inferred from its PASS.

## Known blockers / risks
- The stronger production-path behavioral fault-injection regression still needs to tamper a freshly generated receipt/artifact immediately before independent verification and prove the PASS marker is unreachable for hash, length, path/scope/provider flag and content tampering.
- Existing process harnesses need executable confirmation against the newly mandatory verifier invocation on Windows/PowerShell 7.
- No executable .NET/Windows validation has been performed in this connector environment.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Implement the process-level receipt tamper harness without adding a production bypass hook: execute an isolated copy of the real preflight with fake-dotnet fixtures and a test-only verifier wrapper that tampers the just-created receipt/bound artifact before delegating to an unmodified copy of `verify-preflight-receipt.ps1`; assert no PASS marker for SHA, length, path, scope/provider-live and artifact-content tampering, plus one successful custom-artifact-directory control case.
