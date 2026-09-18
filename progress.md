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
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-18 — browser safety and privacy
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback; rejected embedded URI credentials; added context request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Expanded credential classification across password, OTP, PIN, recovery material, API/access/refresh tokens, identity-number labels and standard security/payment autocomplete tokens. `BrowserObservedValuePrivacyPolicy` became the canonical sensitive-autocomplete contract; production snapshot JavaScript suppresses password/OTP/payment values before reading `el.value`. A hermetic real-Chromium fixture covers synthetic sensitive fields, benign positive controls, accessibility-reference safety and SPA in-place field repurposing.

### 2026-09-18 to 2026-09-19 — executable browser qualification
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated `-SecuritySuite`, explicit high-risk fixture list and deterministic failure propagation. Hardened TRX validation against zero-test, missing evidence, skipped/not-executed, non-passed and partial curated-suite false positives. Failed runs retain forensic TRX evidence; `-KeepResults` retains successful qualification evidence. Caller environment is restored in `finally`. Successful retained runs emit a payload-free `qualification-receipt.json` binding evidence to source commit/dirty state, SDK, configuration, filter, required fixtures and passed test names.

### 2026-09-19 — independent qualification verification
Completed:
- Added `scripts/verify-browser-qualification.ps1`, an independent verifier for retained Chromium qualification evidence rather than trusting the producer script or a screenshot of a green run.
- Verifier requires both the receipt and TRX, schema v1, a real Git source commit, optional exact expected commit, optional clean-source enforcement, at least one TRX result, and all TRX outcomes to be Passed.
- Cross-checks `passedCount` and the exact sorted `passedTests` set against TRX contents, then re-proves every declared curated security fixture has PASS evidence.
- Enforces the payload-free receipt contract by rejecting unexpected top-level fields; future evidence-schema expansion therefore requires an explicit verifier/security review instead of silently carrying browser/session payloads.
- Validates the receipt timestamp and prints only compact provenance/qualification metadata.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/verify-browser-qualification.ps1` (new)
- `progress.md`

Validation/evidence:
- Static review confirms verifier consumes only local retained evidence, performs no network/provider/browser operation, and fails closed for missing source provenance, non-pass TRX outcomes, receipt/TRX disagreement, missing curated fixture evidence, unexpected receipt fields and invalid timestamps.
- Existing producer receipt fields and schema were re-read before implementation; verifier allowlist exactly matches the current producer contract.
- Repository history at run start showed `f9ba05486b2878099604daf8d286531774bd941e` as latest.
- No executable PowerShell/.NET/Chromium PASS is claimed in this connector-only environment.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values before reading DOM values while retaining bounded non-secret semantics needed for safety decisions.
- Browser validation fails closed on process failure, missing/empty evidence, skipped/not-executed evidence, any non-passed result and incomplete curated-suite PASS evidence.
- Qualification receipts are payload-free provenance metadata; independent verification now rejects unreviewed top-level schema expansion and receipt/TRX disagreement.
- Failed TRX evidence is intentionally retained for diagnosis and uses hermetic synthetic fixture data.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking and emergency cancellation remain intact.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- The verifier validates semantic agreement between receipt and TRX but does not yet cryptographically bind the two files; a SHA-256 evidence digest in a versioned receipt is a useful later hardening step.
- Release/judge verification should use `-RequireCleanSource -ExpectedCommit <commit>`; a producer receipt from a dirty checkout remains intentionally representable for developer diagnosis.
- Retained failure directories under OS temp can accumulate until developer cleanup.
- Fixture-presence validation keys off passed test names containing fixture class names; adapter naming changes intentionally fail closed.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
On a clean Windows/.NET 8 Git checkout, run `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`, then independently run `./scripts/verify-browser-qualification.ps1 -EvidenceDirectory <retained-dir> -RequireCleanSource -ExpectedCommit <HEAD>`. Fix any executable browser or verifier failure without weakening safety boundaries; once qualified, move to judge-path authenticated-site compatibility validation.
