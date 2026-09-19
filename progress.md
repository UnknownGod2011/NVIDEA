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

### 2026-09-19 — verifier regression harness and evidence-time provenance
Completed:
- Added `scripts/test-browser-qualification-verifier.ps1`, a hermetic PowerShell regression harness for the security-sensitive evidence verifier. It creates temporary synthetic TRX + schema-v2 receipts and never needs Chromium, provider keys, network access, or paid services.
- Positive canonical evidence plus negative cases cover malformed source provenance, JSON type confusion, reduced security suites, fixture lookalikes, TRX tampering, non-UTC timestamps, implausibly future timestamps and stale evidence under an explicit freshness policy.
- Hardened `validatedAtUtc`: exact invariant round-trip parsing, explicit zero UTC offset, and rejection when more than five minutes in the future.
- Added optional `-MaxEvidenceAgeHours` (1..8760) so release/judge workflows can require fresh evidence without making archival/offline verification expire by default.
- Negative verifier cases execute in isolated child `pwsh` processes so expected terminating errors become observable exit codes rather than aborting the harness; all temporary evidence is removed in `finally`.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/verify-browser-qualification.ps1`
- `scripts/test-browser-qualification-verifier.ps1`
- `progress.md`

Validation/evidence:
- Re-read `progress.md`, recent commits, regression harness and independent verifier before implementation.
- Static review confirms freshness is opt-in, UTC/future validation is always fail-closed, and the harness exercises the new time-provenance boundaries.
- Connector environment cannot execute PowerShell, so the harness remains execution-unverified here; no harness or Chromium PASS is claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values before reading DOM values while retaining bounded non-secret semantics needed for safety decisions.
- Browser validation fails closed on process failure, missing/empty evidence, skipped/not-executed evidence, any non-passed result and incomplete curated-suite PASS evidence.
- Qualification schema v2 binds receipt metadata to exact TRX bytes with SHA-256; independent verification checks byte integrity before semantic evidence.
- Release/judge verification independently pins the canonical five-fixture suite and can now require evidence freshness; UTC/future checks prevent misleading time provenance. Producer metadata cannot redefine completeness.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking and emergency cancellation remain intact.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- The verifier regression harness needs one local PowerShell 7 execution before its PASS can be claimed.
- SHA-256 binds receipt -> TRX integrity but is not a digital signature; anyone able to replace both files can recompute a matching pair. Source-commit matching and clean-checkout enforcement remain required for release/judge evidence.
- Freshness depends on the producer host clock; it prevents accidental stale evidence reuse but is not a cryptographic timestamp authority.
- Canonical fixture constants intentionally exist in producer, verifier and regression harness; suite changes must update all after security review, otherwise checks fail closed.
- Retained failure directories under OS temp can accumulate until developer cleanup.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
On Windows PowerShell 7, run `./scripts/test-browser-qualification-verifier.ps1` and fix any mismatch without weakening fail-closed semantics. Then on a clean Windows/.NET 8 checkout run `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`, followed by `./scripts/verify-browser-qualification.ps1 -EvidenceDirectory <retained-dir> -RequireCleanSource -RequireSecuritySuite -ExpectedCommit <HEAD> -MaxEvidenceAgeHours 24`. Once fresh schema-v2 evidence qualifies, move to judge-path authenticated-site compatibility validation.
