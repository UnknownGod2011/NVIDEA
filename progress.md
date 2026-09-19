# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-18 — browser safety and privacy
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Expanded credential classification across password, OTP, PIN, recovery material, API/access/refresh tokens, identity-number labels and standard security/payment autocomplete tokens. Production snapshots suppress password/OTP/payment values before reading DOM values. Hermetic real-Chromium coverage includes sensitive fields, benign controls, accessibility-reference safety and SPA in-place field repurposing.

### 2026-09-18 to 2026-09-19 — executable browser qualification
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated `-SecuritySuite`, deterministic failure propagation, and fail-closed TRX validation against zero-test, missing evidence, skipped/not-executed, non-passed and partial-suite false positives. Failed runs retain forensic evidence; `-KeepResults` retains successful evidence. Successful retained runs emit a payload-free schema-v2 qualification receipt bound to exact TRX bytes with SHA-256. Added independent `scripts/verify-browser-qualification.ps1` to re-prove source provenance, clean-source policy, exact receipt/TRX test agreement, canonical fixture PASS evidence and payload-free schema constraints. Producer/verifier require complete dot-delimited fixture identities; verifier independently pins the canonical five-fixture suite. Receipt verification rejects missing/unexpected fields, JSON primitive/array type confusion, malformed full commit IDs, malformed string arrays, duplicate/empty PASS identities, reduced suites and locale-dependent timestamps.

### 2026-09-19 — verifier regression harness, evidence-time provenance, release gate
Added a hermetic verifier regression harness covering canonical evidence and malformed provenance/type/suite/lookalike/TRX-tampering/time cases. Hardened evidence timestamps to exact UTC round-trip form with future-date rejection and optional freshness. Added `scripts/verify-release-browser-gate.ps1`, pinning expected GitHub origin, exact clean current HEAD, canonical suite and fresh evidence for release/judge qualification.

### 2026-09-19 — recording-day readiness integration
Completed:
- Integrated browser release qualification directly into `scripts/live-demo-readiness.ps1` via optional `-BrowserEvidenceDirectory` and bounded `-BrowserEvidenceMaxAgeHours` (default 24h).
- When browser evidence is supplied, recording-day readiness invokes the dedicated release gate rather than duplicating verifier policy. The gate therefore remains the single owner of repository-origin, exact-HEAD, clean-checkout, canonical-suite and freshness requirements.
- A failed browser gate becomes an explicit readiness blocker with a do-not-record/release message. Existing behavior remains compatible when no evidence directory is supplied, avoiding accidental breakage of non-browser setup checks.
- Kept the check local/offline: no provider/network operation is introduced by readiness.
- Verified repository metadata immediately before each mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/live-demo-readiness.ps1`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected current live-demo readiness implementation before selecting the task.
- Static review confirms browser qualification is delegated to the existing release gate, preserving a single fail-closed policy path rather than creating a weaker parallel implementation.
- Connector environment cannot execute PowerShell 7/Windows Chromium, so the integrated readiness path remains execution-unverified here; no PowerShell, build or Chromium PASS is claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values before reading DOM values while retaining bounded non-secret semantics needed for safety decisions.
- Browser validation fails closed on process failure, missing/empty evidence, skipped/not-executed evidence, any non-passed result and incomplete curated-suite PASS evidence.
- Qualification schema v2 binds receipt metadata to exact TRX bytes with SHA-256; independent verification checks byte integrity before semantic evidence.
- Release/judge verification independently pins the canonical five-fixture suite and the release gate pins exact repository, clean current HEAD and evidence freshness.
- Recording-day readiness can now require that same release gate, preventing a browser-heavy judge recording from being treated as ready merely because secrets/build/demo-contract checks pass.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking and emergency cancellation remain intact.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release gate and newly integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- Browser evidence remains opt-in to general readiness for backward compatibility; recording instructions should explicitly supply `-BrowserEvidenceDirectory` for the browser demo.
- SHA-256 binds receipt -> TRX integrity but is not a digital signature; anyone able to replace both files can recompute a matching pair. Exact source-commit matching and clean-checkout enforcement remain required.
- Freshness depends on the producer host clock; it is not a cryptographic timestamp authority.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full `live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild -BrowserEvidenceDirectory ...` remain environment-validation items.

## Single Best Next Task
On a clean Windows PowerShell 7 checkout, run the verifier harness, then `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`, then execute `./scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild -BrowserEvidenceDirectory <retained-dir>`. Fix any mismatch without weakening fail-closed semantics. Once this integrated recording-day gate passes, validate the authenticated judge-path browser sites and document the exact recording command in the demo runbook.
