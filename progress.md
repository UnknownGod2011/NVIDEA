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
Added `Nvidea.DemoChecklistGenerator`, `scripts/submission-preflight.ps1`, source/process regressions, fresh artifact materialization, semantic evidence verification, canonical-manifest SHA-256 binding, and a per-invocation receipt binding the accepted local artifacts. The local preflight is validator → checklist → positive evaluator → adversarial evaluator and explicitly excludes provider-live operations. Hardened raw JSON handling against duplicate-property ambiguity before PowerShell conversion.

### 2026-09-17 — strict evidence schema and runtime prerequisites (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value task.
- Added an explicit `System.Text.Json` startup prerequisite probe before any child process is launched. Unsupported PowerShell/.NET environments now receive a clear PowerShell 7/current-runtime remediation message instead of failing later through type resolution.
- Added fail-closed evidence allowlists. Validator evidence accepts only its production schema-v2 fields; evaluator evidence accepts only its production schema-v1 fields; every `checks[]` object is independently allowlisted (`id/passed/requirement` for validator, `id/passed/detail` for evaluators).
- Unknown top-level or check-level properties now fail before semantic PASS acceptance, closing the unreviewed-field/schema-smuggling boundary left after duplicate detection.
- Preserved evaluator `metrics` as an intentionally extensible producer-owned map while requiring the field, when present, to be a JSON object. Metrics are diagnostic and are never used to establish PASS.
- Confirmed the allowlists against the actual `Nvidea.DemoPackageValidator` and `Nvidea.PersonalAiDemoEval` producer record shapes before changing the preflight.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `progress.md`

Validation/evidence:
- Static producer/consumer contract inspection confirms validator output fields are `schemaVersion`, `overallPassed`, `manifestSha256`, `declaredMaximumDurationSeconds`, `plannedDurationSeconds`, `beatCount`, `checks`; validator checks are `id`, `passed`, `requirement`.
- Static positive evaluator inspection confirms output fields are `schemaVersion`, `generatedAt`, `overallPassed`, `checks`, `metrics`; evaluator checks are `id`, `passed`, `detail`.
- Strict duplicate detection still runs on the raw `System.Text.Json` parse tree before allowlist/schema checks and before `ConvertFrom-Json` semantic access.
- Executable PowerShell/.NET validation is not claimed in this connector environment; production preflight and regressions still need execution on the Windows recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Evidence acceptance is now closed against both duplicate-property ambiguity and unexpected-field smuggling at the security-relevant top/check levels.
- `metrics` remains deliberately open because evaluator implementations own evolving diagnostics; it cannot influence PASS and therefore is outside the trusted acceptance surface.
- Parser depth, 1 MiB evidence bound, canonical manifest SHA binding, fresh output deletion/materialization, semantic PASS checks, repository confinement, run receipt hashing, and provider-live exclusion remain intact.
- Startup prerequisite failure occurs before `dotnet` child execution, reducing partial-artifact ambiguity on unsupported recording environments.
- No secret, provider payload, prompt content, or personal data is added by this change.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; run the production preflight plus PowerShell regressions on the actual Windows recording machine before relying on the gate.
- The adversarial evaluator producer shape should be independently re-checked against the evaluator allowlist during the next executable validation pass; it is expected to share the schema-v1 evaluator contract.
- The run receipt is locally generated and unsigned; it proves internal consistency/fresh grouping, not authenticity against a fully compromised local machine.
- Checklist validation checks ordered beat headings and milestones but not every preflight/fallback sentence.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add a dedicated network-free schema-shape fault-injection regression that returns exit 0 with otherwise-valid validator/evaluator evidence containing unexpected top-level and nested check fields, proving the production preflight rejects each case before downstream execution. Then execute the complete preflight regression suite on a real PowerShell 7 + .NET 8 Windows environment when available.
