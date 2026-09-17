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
Added `Nvidea.DemoChecklistGenerator`, `scripts/submission-preflight.ps1`, source/process regressions, fresh artifact materialization, semantic evidence verification, canonical-manifest SHA-256 binding, and a per-invocation receipt binding the accepted local artifacts. The local preflight is validator → checklist → positive evaluator → adversarial evaluator and explicitly excludes provider-live operations.

### 2026-09-17 — strict JSON ambiguity defense (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value task.
- Hardened `scripts/submission-preflight.ps1` so every JSON document entering semantic verification is first parsed with `System.Text.Json.JsonDocument` using comments disallowed, trailing commas disallowed, and max depth 64.
- Added recursive per-object duplicate-property detection with ordinal property-name comparison before `ConvertFrom-Json` is allowed to materialize an object. This prevents parser normalization / last-property-wins behavior from turning ambiguous evidence into an apparent PASS.
- Duplicate detection applies not only to validator/evaluator evidence but also the canonical demo manifest because all pass through `Read-StrictEvidenceJson`.
- Added `scripts/tests/submission-preflight.duplicate-json.ps1`, a network/provider-free fake-`dotnet` fault-injection regression covering duplicate top-level `schemaVersion`, `overallPassed`, `manifestSha256`, and nested `checks[].passed` properties. Every case requires rejection after exactly the validator invocation, proving no checklist/evaluator execution leaks downstream.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `scripts/tests/submission-preflight.duplicate-json.ps1`
- `progress.md`

Validation/evidence:
- Static inspection confirms duplicate traversal occurs on the raw JSON parse tree before PowerShell object conversion.
- The regression fixtures are intentionally authored as raw JSON strings because PowerShell serializers cannot faithfully generate duplicate object properties.
- The regression asserts one and only one fake-dotnet child invocation for each ambiguity case.
- Executable PowerShell/.NET validation is not claimed in this connector environment; these scripts still need execution on the Windows recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Evidence is now fail-closed against duplicate-property schema smuggling at both top-level and nested object depth, closing the previously recorded ambiguity boundary.
- Parser depth and existing 1 MiB evidence-size bounds reduce pathological JSON resource consumption.
- Duplicate comparison is ordinal/case-sensitive, matching JSON property-name semantics; differently-cased names are not treated as duplicates but existing semantic access still requires the expected canonical property names.
- Canonical manifest SHA binding, fresh output deletion/materialization, semantic PASS checks, repository confinement, run receipt hashing, and provider-live exclusion remain intact.
- No secret, provider payload, prompt content, or personal data is added by this change.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; production preflight plus PowerShell regressions need execution on the actual Windows recording machine.
- `System.Text.Json` availability now becomes an explicit preflight runtime requirement in addition to .NET 8; this is expected on the intended modern PowerShell/.NET recording environment but should be surfaced with a clearer prerequisite error rather than a type-resolution failure.
- The run receipt is locally generated and unsigned; it proves internal consistency/fresh grouping, not authenticity against a fully compromised local machine.
- Checklist validation checks ordered beat headings and milestones but not every preflight/fallback sentence.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Make strict parsing operationally robust on the actual Windows recording path: add an explicit startup prerequisite check for `System.Text.Json`/PowerShell compatibility with a clear remediation message, then add strict schema-shape validation that rejects unexpected top-level and nested evidence properties (not just duplicates) so evaluator/validator artifacts cannot smuggle unreviewed fields while still satisfying the minimal PASS contract.
