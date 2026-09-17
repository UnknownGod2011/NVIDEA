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

### 2026-09-17 — manifest-driven judging and zero-cost preflight
Added `Nvidea.DemoChecklistGenerator`, then `scripts/submission-preflight.ps1` with validator → checklist → positive evaluator → adversarial evaluator ordering and explicit provider-live exclusion. Added source and fake-`dotnet` process regressions proving fail-closed sequencing, repository confinement, provider-live absence, and fresh non-empty artifact materialization.

### 2026-09-17 — semantic artifact verification (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value reliability task.
- Hardened `scripts/submission-preflight.ps1` so non-empty output is no longer sufficient evidence of success.
- Validator output is now bounded to 1 MiB, parsed as JSON, required to be schema v2 with `overallPassed: true`, at least one named passing check, and manifest-matching beat count/planned duration.
- Positive and adversarial evaluator outputs are independently parsed and required to be schema v1, explicit PASS, and contain only named passing checks.
- Generated recording checklist is structurally checked against the canonical manifest for exact beat count, order, label, duration, every expected production milestone, and the take-acceptance gate.
- Extended `scripts/tests/submission-preflight.behavior.ps1` to create semantically valid provider-free fixtures from the canonical manifest rather than arbitrary non-empty placeholders.
- Added semantic fault injection: the positive evaluator can lie with exit 0 while writing a non-empty explicit failure document; production preflight must reject it after exactly three child calls and never launch the adversarial evaluator.
- Existing non-zero failure, missing-artifact, successful-sequence, provider-live exclusion, and repository-escape scenarios remain covered.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `scripts/tests/submission-preflight.behavior.ps1`
- `progress.md`

Validation/evidence:
- Inspected the actual validator/evaluator output contracts before implementing semantic checks: validator emits schema v2 `overallPassed`, beat/duration metadata and checks; both local evaluators emit schema v1 `overallPassed` and checks.
- Inspected the checklist generator render contract and matched its numbered beat headings/milestone syntax rather than inventing a parallel format.
- Static review confirms semantic validation occurs immediately after each child and before the next child launch.
- Executable PowerShell/.NET validation is not claimed in this connector environment. The behavior/contract scripts still need execution on the Windows recording machine; no fabricated PASS is recorded.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- A compromised/broken local child can no longer advance preflight merely via exit 0 plus non-empty garbage or an explicit failure JSON document.
- JSON evidence has a pre-parse size bound, reducing accidental memory amplification from malformed local artifacts.
- Checklist semantics are tied back to the canonical manifest, reducing the risk that a stale/wrong operator artifact passes materialization checks.
- Provider-live commands remain outside the zero-cost workflow; no credentials or network behavior were introduced.
- Semantic fixtures are deterministic, local, credential-free and derived only from the repository manifest.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; production preflight plus PowerShell regressions still need execution on the actual Windows recording machine.
- PowerShell `ConvertFrom-Json` is adequate for generated local evidence but this semantic layer does not independently reject duplicate JSON properties; the producer-side validator already rejects duplicate properties in its input manifest. A stricter reusable JSON verifier would improve defense in depth.
- Checklist validation checks ordered beat headings and milestone presence but not every preflight/fallback sentence; the generator itself strictly consumes the validated schema-v2 manifest.
- The unified `Nvidea.JudgingEvidenceVerifier` necessarily consumes fresh Nebius deployment PASS/model-catalog evidence and remains outside zero-cost default preflight.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add cryptographic freshness/binding to local preflight artifacts: independently compute the canonical manifest SHA-256 in PowerShell and require it to equal the validator's `manifestSha256`; add a run nonce or preflight metadata envelope so evaluator/checklist artifacts can be proven to belong to the current invocation rather than merely being freshly written by a child. Extend fault injection with a valid-looking validator document bound to the wrong manifest hash and prove downstream execution is blocked.
