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
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, and process-level fault injection for sequencing, missing outputs, wrong hashes, semantic failures, duplicate fields, and unexpected-field smuggling.

### 2026-09-17 — independent receipt verifier (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value receipt-hardening task.
- Added `scripts/verify-preflight-receipt.ps1`, a network/provider-free independent verifier for `preflight-receipt.json`.
- The verifier rejects duplicate and unknown receipt/manifest/artifact fields, invalid schema/run IDs/timestamps, completion-before-start, provider-live claims, noncanonical manifest paths, manifest hash mismatch, path traversal/rooted/backslash paths, missing artifacts, duplicate/wrong artifact order, malformed/nonpositive lengths, length mismatches, and SHA-256 mismatches.
- Artifact expectations are derived from the confined receipt directory, preserving the production preflight's supported custom `ArtifactsDirectory` behavior instead of hardcoding `artifacts/preflight`.
- Receipt JSON is bounded to 256 KiB and depth 32, disallows comments/trailing commas, and is checked for duplicate properties before PowerShell conversion.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/verify-preflight-receipt.ps1` (new, then corrected for custom artifact directories)
- `progress.md`

Validation/evidence:
- Static review confirms the verifier is local-only and has no provider/network/browser/inference path.
- Static review confirms all receipt-bound files are independently resolved beneath the canonical repository prefix and rehashed from disk.
- The verifier is not yet wired into `submission-preflight.ps1`, so the current production PASS message does not yet depend on this new independent check.
- Executable PowerShell/.NET validation is not claimed in this connector environment; run on the Windows recording machine before relying on the gate.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Receipt verification no longer needs to trust the receipt's own paths/hashes/lengths: every accepted artifact is confined, materialized, length-checked and SHA-256 reverified independently.
- `scope` must be exactly `local-zero-cost` and `providerLiveEvidence` must be boolean false, preventing this receipt class from claiming fresh provider evidence.
- Strict ordered artifact names prevent a receipt from substituting an arbitrary same-directory file while still supporting a custom confined artifact directory.
- The receipt remains unsigned; it establishes local consistency, not authenticity against a fully compromised local machine.
- Existing preflight evidence protections remain intact.

## Known blockers / risks
- The independent receipt verifier still needs integration as the final mandatory step before `submission-preflight.ps1` prints PASS.
- A dedicated fault-injection suite should mutate duplicate/unknown fields, traversal, timestamps, scope/provider-live flags, hashes, lengths and files and prove the verifier fails closed.
- No executable .NET/Windows validation has been performed in this connector environment.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Wire `scripts/verify-preflight-receipt.ps1` into `submission-preflight.ps1` immediately after receipt materialization and before any PASS output, then add a network-free receipt fault-injection regression proving malformed/tampered receipts and artifacts cannot reach the production PASS path.
