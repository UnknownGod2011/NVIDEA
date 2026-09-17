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
Added `scripts/live-demo-readiness.ps1` and strengthened it to require Windows, PowerShell 7+, an installed .NET 8 SDK, solution presence, Nebius/Tavily configuration, optional cloud-research topology/credential presence, accessible configured PEM files without reading their contents, and opt-in `dotnet build --no-restore` validation. Added an always-on schema-v2 demo-package gate for the 180-second limit, unique beats, feature paths, and the exact six production-observed milestone sequence. Extended the gate to validate evidence metadata/files and verification-command projects. Hardened repository asset validation against both lexical traversal and symlink/junction resolved-target escape.

### 2026-09-17 — secure browser transport boundary (latest run)
Completed:
- Re-read this ledger completely and inspected the current repository state, recent commits, browser contracts, browser safety policy, and existing browser tests before changing code.
- Found a concrete production security gap: `BrowserSafetyPolicy` accepted remote `http://` navigation. In an authenticated browser session this could expose cookies, form data, user content, or consequential interaction to a plaintext network path.
- Hardened navigation policy so HTTPS remains allowed, remote plaintext HTTP is blocked, and HTTP loopback endpoints remain available for local development/test flows.
- Added focused xUnit coverage for HTTPS allow, remote HTTP denial, and localhost/127.0.0.1/IPv6-loopback HTTP allowance.
- Explicitly verified repository metadata as `full_name=UnknownGod2011/NVIDEA` before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `tests/Nvidea.Core.Tests/BrowserTransportSafetyTests.cs`
- `progress.md`

Validation/evidence:
- Source review confirms the policy now permits only HTTPS or HTTP destinations for which `Uri.IsLoopback` is true.
- New tests pin the intended transport matrix and ensure remote plaintext denial remains fail-closed.
- No executable .NET test PASS is claimed in this connector environment; the new tests still require execution on a machine with the restored .NET 8 test environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser navigation can no longer intentionally move authenticated/user browser state onto a remote plaintext HTTP origin through the autonomous action path; local loopback development remains supported.
- Existing blocks on script/data/file schemes, autonomous credential typing, and approval gates for consequential or prompt-injection-exposed mutations remain intact.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.
- Manifest-controlled feature, evidence, and command-project paths remain protected against both lexical and resolved-target escape.
- A readiness PASS remains explicitly local evidence, not provider-live evidence.

## Known blockers / risks
- The new browser transport tests and complete unified submission suite still need executable confirmation on Windows/.NET 8/PowerShell 7.
- The strengthened live readiness preflight, including the no-restore build and complete demo-asset gate, needs execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Current connector execution cannot establish live provider or authenticated browser success; those claims must remain unmade until matching production evidence exists.

## Single Best Next Task
On the actual Windows recording machine run the restored core tests including `BrowserTransportSafetyTests`, then `pwsh -File scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild`; resolve every named blocker without copying secret values into logs. Once local gates pass, execute the existing live Nebius/Nemotron + Tavily + authenticated Playwright judge path and capture production-observed evidence for the <=3 minute demo. Prioritize concrete defects revealed by those executions over additional local harness layers.
