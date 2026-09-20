# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-19 — browser safety and qualification
Hardened browser transport to HTTPS or loopback HTTP and WSS or loopback WS; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Added real-Chromium qualification, TRX validation, SHA-256 evidence receipts, independent verification, clean exact-HEAD provenance, release gate and judge-recording gate.

### 2026-09-19 to 2026-09-20 — memory integrity and explicit recovery
Persisted embedding state is treated as untrusted; malformed vector/provenance state is stripped while user-authored memory survives. Migration remains local-provider-only, preserves Sensitive/Restricted opt-ins, revalidates candidates, validates vectors/provenance, skips concurrent edits, and has invalid-vector/failure/cancellation coverage. JSON persistence serializes access, uses same-directory write-through replacement, and maintains one bounded `.bak` last-known-good generation. Recovery is explicit, protection-context validated, fail-closed and never automatic. Startup recovery eligibility uses non-mutating primary validation and cannot be authorized by unrelated startup failure.

### 2026-09-20 — startup/shutdown and partial-construction ownership
WPF cancellation is distinct from corruption/configuration failure. Exit cleanup is contained and idempotent. `StartupResourceLease` provides reverse-order, exactly-once best-effort cleanup until a complete composition root assumes ownership. Deterministic failure coverage exercises failures after memory, Tavily, cloud transfer and pre-release, including cleanup-failure and cancellation semantics.

### 2026-09-20 — desktop Tavily research evidence
Added `DesktopResearchEvidence` as a payload-free evidence boundary over completed reports: source/citation/query/domain/freshness/warning/credit counts plus conservative multi-query, canonical-uniqueness and machine-citation signals.

### 2026-09-20 — restart-stable research provenance receipt
- Durable research checkpoints now carry a SHA-256 lineage from the Nemotron plan into Tavily prepared evidence and final synthesis.
- The evidence commitment is domain-separated by the plan fingerprint (`SHA256(planSha256 + ':' + preparedEvidenceSha256)`), so changing either persisted plan lineage or prepared Tavily evidence fails closed before synthesis.
- Completed jobs expose `DurableResearchReceipt`: plan/evidence/synthesis commitments plus planned-query, evidence-source and validated-citation counts. It contains no question, query, URL, title, snippet, evidence text or answer text.
- Added `DesktopDurableResearchReceipt` projection for judge-facing Windows evidence, including multi-query-plan, machine-citation and restart-stable signals.
- Legacy evidence checkpoints remain resumable but explicitly cannot claim a historical plan commitment (`legacy-unavailable`).
- Added deterministic restart simulation proving synthesis resumes from the persisted evidence checkpoint without another provider call, payload markers do not appear in the receipt, and tampered evidence is rejected before Nemotron synthesis.

Files changed in latest run:
- `src/Nvidea.Core/Jobs/ResearchJobHandler.cs`
- `src/Nvidea.Core/Desktop/DesktopDurableResearchReceipt.cs`
- `tests/Nvidea.Core.Tests/DurableResearchReceiptTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the durable research handler, research engine, Tavily contracts, current tree and recent commits before implementation.
- Static review confirms the receipt is computed from the actual persisted plan/prepared-evidence/final-answer objects, not from demo constants or provider claims.
- Evidence commitment binds the plan hash and full serialized prepared evidence (including source provenance, Tavily credit count and deterministic quality metadata). Synthesis commitment binds the final answer bytes.
- Restart test constructs a new handler after evidence persistence and uses a provider that throws if called, pinning no-repeat Tavily semantics.
- Tamper test changes persisted evidence while retaining its old lineage and requires rejection before any synthesis inference request.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Research receipt fingerprints are one-way commitments, not authentication signatures. Their integrity inherits the protection/authority of the durable checkpoint store; they should be described to judges as restart-stable binding evidence, not third-party attestation.
- Receipt projection deliberately excludes research payloads. Low-entropy payloads could theoretically be guessed against a bare hash, but the public receipt never exposes separate question/query hashes; plan/evidence commitments cover structured high-entropy objects and are intended for equality/integrity evidence, not secrecy by themselves.
- Browser transport, credential authority rejection, consequential-action approvals, sensitive typing blocks, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, bounded, protection-context validated, fail closed and non-mutating during eligibility checks.
- Startup/shutdown cleanup failures cannot replace authoritative startup/exit failures or be projected to UI.

## Known blockers / risks
- New durable-receipt code/tests still require compile/runtime execution under .NET 8; all accumulated Windows suites remain pending executable-environment validation.
- `DesktopDurableResearchReceipt` is a Core projection ready for the judge surface, but the WPF Judge Evidence dialog does not yet render completed durable-research receipts.
- Receipt commitments prove persisted-stage consistency, not truth of web sources and not cryptographic third-party attestation.
- Real-Chromium fixtures and release/judge qualification scripts still need execution on Windows with .NET 8, PowerShell 7 and matching Playwright Chromium.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Wire completed `DesktopDurableResearchReceipt` values into the existing WPF Judge Evidence dialog/session evidence path with concise plan -> Tavily evidence -> cited synthesis labels, then add projection/UI-state tests that prove no research payload can cross that judge-facing boundary.
