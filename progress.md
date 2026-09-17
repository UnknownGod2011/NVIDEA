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
Added `scripts/live-demo-readiness.ps1` and strengthened it to require Windows, PowerShell 7+, an installed .NET 8 SDK, solution presence, Nebius/Tavily configuration, optional cloud-research topology/credential presence, accessible configured PEM files without reading their contents, and opt-in `dotnet build --no-restore` validation. Added an always-on schema-v2 demo-package gate for the 180-second limit, unique beats, feature paths, and the exact six production-observed milestone sequence. Extended the gate to validate evidence metadata/files and verification-command projects.

### 2026-09-17 — symlink-safe demo asset boundary (latest run)
Completed:
- Re-read this ledger completely and inspected the current recording readiness implementation and recent repository commits before changing code.
- Found a concrete path-boundary weakness: `Test-RepositoryPath` used `GetFullPath` lexical confinement followed by `Test-Path`. A repository-local symlink or Windows junction could therefore lexically remain under NVIDEA while resolving to a target outside the checkout.
- Hardened `Test-RepositoryPath` with two-stage confinement: lexical normalization must remain beneath the checkout, then the existing target is resolved with `Resolve-Path` and the resolved target must also remain beneath the resolved repository root.
- This applies uniformly to demo feature paths, judge evidence files, and verification-command project paths.
- Repository metadata was explicitly re-verified as `full_name=UnknownGod2011/NVIDEA` immediately before the final GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/live-demo-readiness.ps1`
- `progress.md`

Validation/evidence:
- Source review confirms repository-controlled asset paths now receive both lexical and resolved-target confinement before readiness accepts them.
- Existing missing-file and leaf checks remain intact after resolution.
- No executable Windows PASS is claimed in this connector environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Secret environment values remain presence-only and are never printed, persisted, hashed, or measured.
- Private/public PEM files are checked for existence/accessibility without reading content or exposing configured paths in errors.
- Manifest-controlled feature, evidence, and command-project paths are now protected against both `..`/rooted lexical escape and symlink/junction resolved-target escape.
- Build validation cannot implicitly restore dependencies because it remains pinned to `--no-restore`.
- A readiness PASS remains explicitly local evidence, not provider-live evidence.
- No production runtime/provider adapter was weakened or replaced.

## Known blockers / risks
- The complete unified submission suite still needs executable confirmation on Windows/PowerShell 7.
- The strengthened live readiness preflight, including the no-restore build and symlink-safe complete demo-asset gate, needs execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Current connector execution cannot establish live provider or authenticated browser success; those claims must remain unmade until matching production evidence exists.

## Single Best Next Task
On the actual Windows recording machine run `pwsh -File scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild`; resolve every named blocker without copying secret values into logs. Then run the unified submission suite and, once both local gates pass, execute the existing live Nebius/Tavily/authenticated-browser judge path and capture production-observed evidence for the <=3 minute demo. Do not add more local harness layers unless those executions reveal a concrete defect.
