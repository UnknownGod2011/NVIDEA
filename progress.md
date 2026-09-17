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

### 2026-09-17 — recording-day readiness hardening
Added `scripts/live-demo-readiness.ps1` and strengthened it to require Windows, PowerShell 7+, an installed .NET 8 SDK, solution presence, Nebius/Tavily configuration, optional cloud-research topology/credential presence, accessible configured PEM files without reading their contents, and opt-in `dotnet build --no-restore` validation. Added an always-on schema-v2 demo-package gate for the 180-second limit, unique beats, feature paths, and the exact six production-observed milestone sequence.

### 2026-09-17 — complete demo asset readiness gate (latest run)
Completed:
- Re-read this ledger completely, inspected the latest commits, `docs/demo-package.json`, and the current recording readiness implementation before changing code.
- Found a concrete readiness gap: beat feature paths were validated, but judge evidence paths and verification-command project paths were trusted without filesystem/boundary checks. A stale or escaping evidence/command path could therefore survive local readiness and fail during recording.
- Refactored repository path validation into one canonical `Test-RepositoryPath` boundary and applied it to feature paths, every evidence artifact, and every verification command project.
- Evidence entries must now carry a non-empty evidence class and claim, and evidence paths must resolve to regular files inside NVIDEA.
- Verification commands must now exist, have unique/non-empty labels, point to regular project files inside NVIDEA, contain non-empty command text, and actually reference their declared project path.
- Re-verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/live-demo-readiness.ps1`
- `progress.md`

Validation/evidence:
- Inspected the current schema-v2 demo package: its evidence paths are repository-relative documentation files and its five verification commands declare repository-relative `.csproj` paths that their command strings reference.
- Source review confirms all manifest-controlled filesystem probes now canonicalize under the NVIDEA repository before existence checks.
- No executable Windows PASS is claimed in this connector environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Secret environment values remain presence-only and are never printed, persisted, hashed, or measured.
- Private/public PEM files are checked for existence/accessibility without reading content or exposing configured paths in errors.
- Manifest-controlled feature, evidence, and command-project paths are canonicalized and confined to NVIDEA, preventing the readiness manifest from probing arbitrary filesystem locations.
- Build validation cannot implicitly restore dependencies because it remains pinned to `--no-restore`.
- A readiness PASS remains explicitly local evidence, not provider-live evidence.
- No production runtime/provider adapter was weakened or replaced.

## Known blockers / risks
- The complete unified submission suite still needs executable confirmation on Windows/PowerShell 7.
- The strengthened live readiness preflight, including the no-restore build and complete demo-asset gates, needs execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Current connector execution cannot establish live provider or authenticated browser success; those claims must remain unmade until matching production evidence exists.

## Single Best Next Task
On the actual Windows recording machine run `pwsh -File scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild`; resolve every named blocker without copying secret values into logs. Then run the unified submission suite and, once both local gates pass, execute the existing live Nebius/Tavily/authenticated-browser judge path and capture production-observed evidence for the <=3 minute demo. Do not add more local harness layers unless those executions reveal a concrete defect.
