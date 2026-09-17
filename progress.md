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

### 2026-09-17 — secure browser transport boundary
Blocked intentional remote plaintext HTTP navigation while preserving HTTPS and HTTP loopback development endpoints. Added focused transport-policy coverage.

### 2026-09-17 — post-action redirect transport enforcement (latest run)
Completed:
- Re-read this ledger completely and inspected current browser contracts, safety policy, executor flow, and browser tests before changing code.
- Found a second transport gap: pre-action URL policy cannot prevent a permitted HTTPS page or ordinary click from redirecting the browser to remote plaintext HTTP (or another unsafe scheme) after the driver action.
- Added `BrowserSafetyPolicy.EvaluateObservedLocation` so the same HTTPS/loopback-only transport invariant can be applied to actual post-action browser observations.
- Hardened `BrowserAgentExecutor` to evaluate the observed URL immediately after execution and before postcondition verification. Unsafe resulting locations now produce a blocked, unverified receipt and prevent subsequent plan actions from running.
- The unsafe-location path deliberately does not invoke the normal verifier, so a content/title/snapshot change on an unsafe destination cannot accidentally turn the action into a verified success.
- Added regression coverage proving a click that lands on remote HTTP is blocked, the verifier is not called, and a multi-action plan stops immediately.
- Explicitly verified repository metadata as `full_name=UnknownGod2011/NVIDEA` before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `src/Nvidea.Core/Browser/BrowserSafetyPolicy.cs`
- `src/Nvidea.Core/Browser/BrowserAgentExecutor.cs`
- `tests/Nvidea.Core.Tests/BrowserAgentTests.cs`
- `progress.md`

Validation/evidence:
- Source review confirms post-action URL safety runs after the second observation and strictly before `_verifier.VerifyAsync`.
- The executor returns `DriverReportedSuccess=true` but `Verified=false` for an unsafe redirect, accurately distinguishing that the browser driver performed the action while the autonomous task did not safely complete.
- `ExecutePlanAsync` already stops on any unverified receipt, so unsafe redirect detection terminates the remaining autonomous plan without adding another control path.
- No executable .NET test PASS is claimed in this connector environment; new and existing browser tests still require execution on a restored .NET 8 environment.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Both requested navigation destinations and actual post-action destinations are now subject to the same HTTPS-or-loopback transport boundary.
- Redirects caused by clicks, forms, refreshes, navigation, or other browser actions cannot be treated as verified success when they land on remote plaintext or unsafe schemes.
- Existing blocks on autonomous credential typing and approval gates for consequential or prompt-injection-exposed mutations remain intact.
- Secret environment values remain presence-only in readiness tooling and are never printed, persisted, hashed, or measured.
- Manifest-controlled feature, evidence, and command-project paths remain protected against both lexical and resolved-target escape.
- A readiness PASS remains explicitly local evidence, not provider-live evidence.

## Known blockers / risks
- Browser transport/redirect tests and the complete unified submission suite still need executable confirmation on Windows/.NET 8/PowerShell 7.
- The strengthened live readiness preflight, including the no-restore build and complete demo-asset gate, needs execution on the actual Windows recording machine.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Post-action detection cannot retroactively prevent the network navigation itself when a trusted page issues a redirect; it prevents verification and further autonomous interaction. Browser-context/network-layer request interception is the stronger future defense if the Playwright driver does not already enforce it.
- Current connector execution cannot establish live provider or authenticated browser success; those claims must remain unmade until matching production evidence exists.

## Single Best Next Task
Inspect the production Playwright driver/context configuration and, if not already present, enforce the same HTTPS-or-loopback rule at the browser request/navigation layer so unsafe redirect requests are aborted before remote plaintext transport occurs. Add focused driver-level tests without weakening authenticated-session behavior. Then run the restored browser tests and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` on the actual Windows recording machine before live judge-path capture.
