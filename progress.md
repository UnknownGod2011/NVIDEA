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
- Added `DesktopResearchEvidence` and `DesktopResearchEvidenceProjector` as a payload-free evidence boundary over completed research reports.
- The Windows-facing `DesktopInvocationResult` can now project source count, validated citation count, unique planned-query count, unique-host diversity, unknown-publication-time count, warning count, Tavily provider credits, multi-query evidence, canonical-source uniqueness and machine-verifiable citation presence without exposing questions, answers, URLs, titles, snippets or provider payloads.
- Added deterministic tests for multi-query/diverse-source/citation evidence, duplicate canonical-source detection, unknown freshness, and projection directly through the desktop invocation result.

Files changed in latest run:
- `src/Nvidea.Core/Desktop/DesktopResearchEvidence.cs`
- `tests/Nvidea.Core.Tests/DesktopResearchEvidenceTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected current research engine/Tavily contracts and Windows desktop invocation path, and reviewed recent commits before implementation.
- Static review confirms the evidence projection is derived only from the completed `ResearchReport`; no network call, secret, query text, URL, title, snippet or answer is persisted/projected by the new type.
- Tests deliberately use private marker strings and assert they do not appear in the projected evidence representation; duplicate canonical URLs fail the uniqueness signal rather than being presented as deduplicated proof.
- `PublishedAt == null` is conservatively reported as freshness unknown; the projector does not claim that timestamped evidence is necessarily fresh or that unknown evidence is stale.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential authority rejection, consequential-action approvals, sensitive typing blocks, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, bounded, protection-context validated, fail closed and non-mutating during eligibility checks.
- Startup/shutdown cleanup failures cannot replace authoritative startup/exit failures or be projected to UI.
- Research evidence is deliberately payload-free and conservative: counts/signals are evidence of runtime structure, not proof that sources are true. Missing publication timestamps remain unknown rather than being promoted to freshness claims.

## Known blockers / risks
- The new desktop research evidence tests still need compile/runtime execution under .NET 8; all accumulated Windows suites remain pending executable-environment validation.
- Multi-query evidence currently proves that returned sources span multiple distinct query strings; a later end-to-end checkpoint receipt should bind this to the original Nemotron plan even when a planned query returns zero sources.
- Canonical uniqueness is an explicit signal, not a substitute for semantic near-duplicate detection.
- Resumable durable research state still needs a single judge-facing receipt that survives restart and binds plan -> gathered evidence -> synthesis without leaking payloads.
- Real-Chromium fixtures and release/judge qualification scripts still need execution on Windows with .NET 8, PowerShell 7 and matching Playwright Chromium.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Extend durable research checkpoints with a payload-free, restart-stable receipt binding the Nemotron plan fingerprint, Tavily evidence/canonical-source fingerprint, synthesis citation validation and resume state; surface that receipt through the desktop judge-evidence path so the <=3 minute demo can prove Tavily multi-query research and resumability rather than merely claim them.
