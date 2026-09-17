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
Added manifest-driven checklist generation and a zero-cost validator → checklist → positive evaluator → adversarial evaluator preflight. Hardened it with fresh artifact enforcement, semantic PASS checks, canonical manifest SHA-256 binding, run receipts, duplicate-property rejection, strict evidence allowlists, System.Text.Json prerequisite checks, process-level fault injection, and an independent receipt verifier. The independent verifier is mandatory before production PASS. Added static and behavioral receipt-tamper regressions without adding a production bypass/test hook. Added `scripts/test-submission-tooling.ps1` as the deterministic single entry point for all six zero-cost submission regressions, with optional repository-confined machine-readable evidence capture.

### 2026-09-17 — live demo readiness preflight (latest run)
Completed:
- Re-read this ledger completely and inspected the current repo tree, recent commits, Tavily implementation/tests, and live Nebius research configuration before changing anything.
- Added `scripts/live-demo-readiness.ps1`, a zero-network, credential-safe recording-machine preflight that checks Windows, PowerShell 7+, dotnet availability/.NET 8 expectation, solution presence, and presence of the core `NEBIUS_API_KEY` + `TAVILY_API_KEY` configuration.
- Added opt-in `-RequireCloudResearch` validation for the exact Nebius Serverless/Object Storage topology, MysteryBox secret-reference, PEM-file, and provider-credential environment-variable names consumed by `NebiusResearchLiveConfigurationLoader`.
- The preflight never prints, hashes, measures, serializes, or persists secret values. It reports variable names only and explicitly states that PASS proves configuration presence, not provider connectivity.
- Re-verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/live-demo-readiness.ps1`
- `progress.md`

Validation/evidence:
- Source-level inspection confirms the cloud variable inventory is derived from the production live configuration loader rather than guessed names.
- Existing Tavily implementation and tests were inspected; no API behavior was changed because fresh official Tavily search results were unavailable and stale assumptions were not substituted.
- No executable Windows PASS is claimed in this connector environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The new readiness script is presence-only and intentionally cannot leak secret values through output/evidence.
- Cloud validation keeps topology/configuration names before provider credential names, matching the production loader's least-exposure ordering.
- A readiness PASS is explicitly not provider-live evidence; live integration still requires the existing real probes and judge evidence surfaces.
- No production runtime or provider adapter was weakened or replaced.

## Known blockers / risks
- The complete unified submission suite still needs executable confirmation on Windows/PowerShell 7.
- The new live readiness preflight also needs execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
On the actual Windows recording machine first run `pwsh -File scripts/live-demo-readiness.ps1 -RequireCloudResearch`; resolve every named blocker without copying secret values into logs. Then run `pwsh -File scripts/test-submission-tooling.ps1 -ContinueOnFailure -EvidencePath artifacts/submission-tooling/windows-diagnostic.json`, fix every failure, and produce the fail-fast PASS evidence. Once both local gates pass, execute the existing live Nebius/Tavily/browser judge path and capture production-observed evidence for the <=3 minute demo.
