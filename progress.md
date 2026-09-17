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
Added `Nvidea.DemoChecklistGenerator`, `scripts/submission-preflight.ps1`, source/process regressions, fresh artifact materialization, and semantic evidence verification. The local preflight is validator → checklist → positive evaluator → adversarial evaluator and explicitly excludes provider-live operations.

### 2026-09-17 — cryptographic preflight binding (latest run)
Completed:
- Re-read this ledger completely and implemented the recorded highest-value task.
- `scripts/submission-preflight.ps1` now independently computes SHA-256 over the canonical `docs/demo-package.json` bytes and requires exact agreement with validator `manifestSha256`; a valid-looking validator report for any other manifest fails before checklist/evaluator execution.
- Added a per-invocation cryptographically bound `preflight-receipt.json` containing a fresh random run ID, UTC start/completion timestamps, canonical manifest SHA-256, and SHA-256 + byte length + repository-relative path for all four freshly generated local artifacts.
- Receipt explicitly records `scope=local-zero-cost` and `providerLiveEvidence=false`; it cannot be mistaken for fresh Nebius/Tavily/browser evidence.
- Extended `scripts/tests/submission-preflight.behavior.ps1`: positive validator fixture now derives the real canonical manifest SHA-256; added wrong-hash fault injection proving only the validator child runs; successful path verifies receipt identity, manifest binding, four artifact bindings, provider-live falsehood prevention, and recomputes every receipt artifact hash from disk.
- Existing non-zero failure, withheld artifact, semantic failure, ordered-success, provider-live exclusion, and repository escape tests remain represented.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation. No other repository was mutated.

Files changed this run:
- `scripts/submission-preflight.ps1`
- `scripts/tests/submission-preflight.behavior.ps1`
- `progress.md`

Validation/evidence:
- Static inspection confirms manifest hash verification executes immediately after validator semantic validation and before checklist generation.
- Static inspection confirms the receipt is created only after all four children have produced fresh semantically accepted artifacts.
- Behavior fixture derives SHA-256 using PowerShell `Get-FileHash`, matching production's byte-level SHA-256 mechanism rather than a reserialized JSON representation.
- Executable PowerShell/.NET validation is not claimed in this connector environment; the behavior/contract scripts still need execution on the Windows recording machine.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Validator evidence is now cryptographically tied to the exact canonical manifest bytes, closing wrong-manifest replay despite otherwise matching schema/count/duration.
- Fresh child outputs were already deleted before execution; the final receipt now binds those accepted outputs into one current-run evidence set with a unique invocation identity.
- Receipt hashes detect later mutation/substitution of local artifacts. The receipt is an integrity/binding record, not a signature or external attestation; a local attacker able to rewrite both artifacts and receipt remains outside this threat boundary.
- No secret, provider payload, prompt content, or personal data is added to the receipt.
- Provider-live commands remain outside zero-cost preflight and receipt explicitly denies provider-live evidence.

## Known blockers / risks
- No executable .NET/Windows validation has been performed in this connector environment; production preflight plus PowerShell regressions need execution on the actual Windows recording machine.
- PowerShell `ConvertFrom-Json` does not independently reject duplicate properties in generated evidence. A strict reusable evidence parser would improve defense in depth.
- The run receipt is locally generated and unsigned; it proves internal consistency/fresh grouping, not authenticity against a fully compromised local machine.
- Checklist validation checks ordered beat headings and milestones but not every preflight/fallback sentence.
- Unified judging verification and live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Harden the local evidence parser against duplicate JSON properties and schema smuggling. Implement a small network-free strict JSON verification utility (or PowerShell parser using `System.Text.Json` with explicit duplicate-property traversal) shared by validator/evaluator evidence checks, then fault-inject duplicate `overallPassed`, `schemaVersion`, `manifestSha256`, and check fields to prove ambiguous evidence fails closed before downstream execution.
