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
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated `-SecuritySuite`, deterministic failure propagation, and fail-closed TRX validation against zero-test, missing evidence, skipped/not-executed, non-passed and partial-suite false positives. Failed runs retain forensic evidence; `-KeepResults` retains successful evidence. Successful retained runs emit a payload-free qualification receipt with source/configuration/test provenance. Added independent `scripts/verify-browser-qualification.ps1` to re-prove source provenance, clean-source policy, exact receipt/TRX test agreement, curated fixture PASS evidence and payload-free schema constraints. Schema v2 binds the receipt to the exact TRX bytes with SHA-256 and verifies that digest before parsing test semantics.

### 2026-09-19 — independent canonical security-suite definition
Completed:
- Hardened `scripts/verify-browser-qualification.ps1` so it owns an independent canonical list of the five required Chromium security fixtures rather than trusting `requiredFixtures` supplied by the evidence producer.
- Security-suite receipts must now declare exactly that canonical set. A producer bug or modification that emits `securitySuite=true` with a reduced fixture list fails independent verification even if the receipt and TRX otherwise agree.
- Canonical fixture constants, rather than receipt-controlled strings, are used for PASS-name matching, removing wildcard interpretation of untrusted fixture metadata from the proof step.
- Preserved schema-v2 TRX SHA-256 binding, exact receipt/TRX PASS-name/count agreement, clean-source/expected-commit controls, payload-free field allowlist and fail-closed non-passed-result handling.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/verify-browser-qualification.ps1`
- `progress.md`

Validation/evidence:
- Re-read `progress.md`, recent commits, producer and verifier before modification.
- Static contract review identified and closed a real independence gap: previously the verifier accepted whichever non-empty unique fixture set the producer declared as the security suite.
- Static review confirms exact set equality is order-independent and canonical fixture names are constants.
- No executable PowerShell/.NET/Chromium PASS is claimed in this connector-only environment.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values before reading DOM values while retaining bounded non-secret semantics needed for safety decisions.
- Browser validation fails closed on process failure, missing/empty evidence, skipped/not-executed evidence, any non-passed result and incomplete curated-suite PASS evidence.
- Qualification schema v2 binds receipt metadata to exact TRX bytes with SHA-256; independent verification checks byte integrity before semantic evidence.
- Release/judge verification independently pins the canonical five-fixture suite; the producer cannot redefine completeness through receipt metadata.
- Receipt remains payload-free and verifier rejects unreviewed top-level schema expansion.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking and emergency cancellation remain intact.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- SHA-256 binds receipt -> TRX integrity but is not a digital signature; anyone able to replace both files can recompute a matching pair. Source-commit matching and clean-checkout enforcement remain required for release/judge evidence.
- The canonical fixture list currently exists in both producer and independent verifier by design; intentional suite changes must update both after security review, otherwise verification fails closed.
- Release/judge verification should use `-RequireCleanSource -RequireSecuritySuite -ExpectedCommit <commit>`; dirty-checkout or custom-filter evidence remains useful only for developer diagnosis.
- Retained failure directories under OS temp can accumulate until developer cleanup.
- Fixture-presence validation keys off passed test names containing canonical fixture class names; adapter naming changes intentionally fail closed.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
On a clean Windows/.NET 8 Git checkout, run `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`, then independently run `./scripts/verify-browser-qualification.ps1 -EvidenceDirectory <retained-dir> -RequireCleanSource -RequireSecuritySuite -ExpectedCommit <HEAD>`. Fix any executable browser or verifier failure without weakening safety boundaries; once schema-v2 evidence qualifies, move to judge-path authenticated-site compatibility validation.
