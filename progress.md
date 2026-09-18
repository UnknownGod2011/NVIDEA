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
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated `-SecuritySuite`, explicit high-risk fixture list and deterministic failure propagation. Hardened TRX validation against zero-test, missing evidence, skipped/not-executed, non-passed and partial curated-suite false positives. Failed runs retain forensic TRX evidence; `-KeepResults` retains successful qualification evidence. Caller environment is restored in `finally`.

### 2026-09-19 — qualification provenance receipt
Completed:
- Added a payload-free `qualification-receipt.json` beside successful browser TRX evidence. It records schema version, UTC validation time, exact source commit when Git is available, dirty-checkout state, .NET SDK, configuration, effective VSTest filter, pass count, required curated fixtures and passed test names.
- Retained qualification evidence can now be tied to the source/configuration that produced it instead of relying on a directory or screenshot with ambiguous provenance.
- A dirty checkout is not silently presented as release evidence: the receipt records `sourceDirty=true` and retained successful runs emit a warning to commit/re-run before release qualification.
- The receipt deliberately excludes DOM values, cookies, credentials, browser payloads and provider secrets.
- Git absence does not block local developer validation; provenance fields remain null. This avoids making Git installation a runtime prerequisite while making the evidence limitation explicit.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/run-browser-integration.ps1`
- `progress.md`

Validation/evidence:
- Static inspection confirms the runner still targets `tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj`, uses the generated Playwright installer, scopes `NVIDEA_RUN_BROWSER_INTEGRATION=1` to test execution, and validates TRX PASS evidence before writing the receipt.
- Receipt generation happens only after zero-result/non-pass/required-fixture checks, so a failed or incomplete run cannot receive a successful qualification receipt.
- Repository history at run start showed `89c9cd048c5941239c58c3a15e79d050253578ac` as latest before implementation.
- No executable .NET or Chromium PASS is claimed in this connector-only environment.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values before reading DOM values while retaining bounded non-secret semantics needed for safety decisions.
- Browser validation fails closed on process failure, missing/empty evidence, skipped/not-executed evidence, any non-passed result and incomplete curated-suite PASS evidence.
- Qualification receipts are payload-free provenance metadata; they do not duplicate sensitive browser state.
- Failed TRX evidence is intentionally retained for diagnosis and uses hermetic synthetic fixture data.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking and emergency cancellation remain intact.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- If Git is unavailable, receipt source provenance is null; release/judge qualification should be run from a Git checkout so the receipt binds to a commit.
- A dirty checkout may pass but is explicitly marked non-clean; release evidence should be rerun after committing.
- Retained failure directories under OS temp can accumulate until developer cleanup.
- Fixture-presence validation keys off passed test names containing fixture class names; adapter naming changes intentionally fail closed.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and `scripts/live-demo-readiness.ps1 -RequireCloudResearch -ValidateBuild` remain environment-validation items.

## Single Best Next Task
On a clean Windows/.NET 8 Git checkout, run `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`. Inspect both `browser-integration.trx` and `qualification-receipt.json`; require the receipt to show the expected commit and `sourceDirty=false`. Fix any real-browser/TRX failure without weakening safety boundaries, record the executable evidence, then move to judge-path authenticated-site compatibility validation.
