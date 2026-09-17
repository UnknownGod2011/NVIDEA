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
Added `Nvidea.DemoChecklistGenerator`, `scripts/submission-preflight.ps1`, source/process regressions, fresh artifact materialization, semantic evidence verification, canonical-manifest SHA-256 binding, and a per-invocation receipt binding the accepted local artifacts. The local preflight is validator → checklist → positive evaluator → adversarial evaluator and explicitly excludes provider-live operations. Hardened raw JSON handling against duplicate-property ambiguity before PowerShell conversion, added strict evidence allowlists, and added a recording-machine `System.Text.Json` prerequisite probe.

### 2026-09-17 — unexpected-field fault injection (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value task.
- Added `scripts/tests/submission-preflight.schema-shape.ps1`, a dedicated network/provider-free fake-`dotnet` process regression for the production preflight.
- Added otherwise-valid exit-0 validator fixtures containing an unexpected top-level trust-like property and an unexpected nested `checks[]` property. Each case must fail after exactly one child invocation, proving checklist/evaluator execution is blocked.
- Added otherwise-valid exit-0 positive-evaluator fixtures containing unexpected top-level and nested `checks[]` properties. Each case must fail after exactly three child invocations, proving the adversarial evaluator is never reached.
- Fixtures retain canonical manifest SHA, beat count, duration and explicit PASS values so the regression isolates schema-shape rejection rather than succeeding because of an unrelated semantic/hash failure.
- The harness uses a temporary PATH-local `dotnet.cmd`, restores all environment variables in `finally`, removes temporary/repository test artifacts, and performs no provider, credential, browser, inference or network operation.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before each GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/tests/submission-preflight.schema-shape.ps1` (new)
- `progress.md`

Validation/evidence:
- Static inspection confirms the regression invokes the real `scripts/submission-preflight.ps1` and shadows only its `dotnet` children.
- Validator unexpected-field scenarios assert exactly 1 child call; positive-evaluator unexpected-field scenarios assert exactly 3 child calls.
- Expected failures are tied to the production error path `unexpected JSON property 'unexpectedTrustSignal'` at either `$` or `$.checks[0]`.
- Executable PowerShell/.NET validation is not claimed in this connector environment; this new regression and the existing preflight suite still need execution on the Windows recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Evidence acceptance is covered against duplicate-property ambiguity and unexpected-field smuggling at both security-relevant top-level and per-check objects, with process-level tests now targeting the latter.
- `metrics` remains deliberately open because evaluator implementations own evolving diagnostics; it cannot influence PASS and therefore is outside the trusted acceptance surface.
- Parser depth, 1 MiB evidence bound, canonical manifest SHA binding, fresh output deletion/materialization, semantic PASS checks, repository confinement, run receipt hashing, and provider-live exclusion remain intact.
- Fault fixtures are intentionally valid on all preceding trusted fields, reducing false confidence from tests that fail for the wrong reason.
- No secret, provider payload, prompt content, or personal data is added by this change.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; run the production preflight plus all PowerShell regressions on the actual Windows recording machine before relying on the gate.
- The adversarial evaluator producer shape should still be independently re-checked against the evaluator allowlist during the next executable validation pass.
- The run receipt is locally generated and unsigned; it proves internal consistency/fresh grouping, not authenticity against a fully compromised local machine.
- Checklist validation checks ordered beat headings and milestones but not every preflight/fallback sentence.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Harden the receipt itself as a strict, independently verifiable evidence artifact: add a network-free receipt verifier that rejects duplicate/unknown fields, path traversal, missing/hash-mismatched artifacts, wrong canonical manifest hash, provider-live claims, malformed timestamps/run IDs, and artifacts outside the repository. Then integrate that verifier as the final local preflight self-check so a malformed receipt can never be reported as PASS.
