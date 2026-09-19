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
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated `-SecuritySuite`, deterministic failure propagation, fail-closed TRX validation, and payload-free schema-v2 qualification receipts bound to exact TRX bytes with SHA-256. Added independent `scripts/verify-browser-qualification.ps1` to re-prove source provenance, clean-source policy, exact receipt/TRX agreement, canonical fixture PASS evidence and payload-free schema constraints. Producer/verifier require complete fixture identities; verifier independently pins the canonical five-fixture suite. Receipt verification rejects schema/type confusion, malformed commit IDs/string arrays, duplicate/empty PASS identities, reduced suites and invalid timestamps.

### 2026-09-19 — verifier/release/recording qualification
Added hermetic verifier regression coverage for canonical evidence plus malformed provenance/type/suite/lookalike/TRX-tampering/time cases. Hardened evidence timestamps to exact UTC round-trip form with future-date rejection and optional freshness. Added `scripts/verify-release-browser-gate.ps1`, pinning expected GitHub origin, exact clean current HEAD, canonical suite and fresh evidence. Integrated that release qualification into `scripts/live-demo-readiness.ps1` and updated the judge runbook to require verifier regression, a fresh retained real-Chromium security-suite run, and integrated readiness before recording.

### 2026-09-19 — fail-closed judge recording entry point
Added `scripts/judge-recording-gate.ps1`: browser evidence is mandatory, Windows/PowerShell 7 are required, cloud-research readiness is always enabled, build validation is default-on, and subordinate exit statuses are independently enforced across child `pwsh` processes.

### 2026-09-19 — verifier self-test is now part of the recording trust chain
Completed:
- Hardened `scripts/judge-recording-gate.ps1` so the hermetic browser qualification verifier regression harness runs by default before any retained browser evidence is trusted.
- The regression harness executes in its own `pwsh` child process and any non-zero status blocks recording immediately. This proves the verifier's canonical-positive and tamper/type/suite/time rejection behavior on the recording machine before the release verifier is relied upon.
- Added `-SkipVerifierRegression` only as an explicit diagnostic override and emit a warning when it is used. The default final-recording path remains fail closed.
- Added an explicit warning for `-SkipBuildValidation` as well, making both weakening overrides visible rather than silent.
- Verified repository metadata immediately before every mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/judge-recording-gate.ps1`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely; inspected recent commits/repository tree plus the recording gate and integrated live-demo readiness implementation before changing code.
- Static review confirmed the recording wrapper now orders trust establishment as verifier self-test -> integrated readiness -> release evidence verification/build/cloud configuration.
- Child-process boundaries preserve subordinate scripts' intentional `exit` contracts while allowing the recording gate to independently reject non-zero statuses.
- Connector environment cannot execute PowerShell 7/Windows Chromium, so no script/build/Chromium PASS is claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Request transport permits HTTPS or HTTP loopback only; WebSockets permit WSS or WS loopback only; credential-bearing authority fields fail closed.
- Consequential actions require approval; sensitive autonomous typing is blocked across passwords, OTP/verification codes, payment credentials, private/recovery keys, API/access/refresh tokens and identity-number labels.
- Browser observations suppress password/OTP/payment values before reading DOM values while retaining bounded non-secret semantics needed for safety decisions.
- Browser validation fails closed on process failure, missing/empty evidence, skipped/not-executed evidence, any non-passed result and incomplete curated-suite PASS evidence.
- Qualification schema v2 binds receipt metadata to exact TRX bytes with SHA-256; independent verification checks byte integrity before semantic evidence.
- Release verification independently pins canonical fixtures, exact repository, clean current HEAD and evidence freshness.
- The judge-specific gate makes browser evidence structurally mandatory and now self-tests the evidence verifier before trusting it; diagnostic weakening overrides are explicit and warned.
- Prompt-injection gates, quarantine, audit boundaries, Service Worker blocking and emergency cancellation remain intact.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- SHA-256 binds receipt -> TRX integrity but is not a digital signature; anyone able to replace both files can recompute a matching pair. Exact source-commit matching and clean-checkout enforcement remain required.
- Freshness depends on the producer host clock; it is not a cryptographic timestamp authority.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
On the clean Windows recording checkout, produce fresh evidence with `./scripts/run-browser-integration.ps1 -InstallChromium -SecuritySuite -KeepResults`, then run only the canonical final entry point `./scripts/judge-recording-gate.ps1 -BrowserEvidenceDirectory <retained-browser-evidence-directory>` without diagnostic skip flags. The gate now self-tests the verifier automatically. Fix any mismatch without weakening fail-closed semantics; once it passes, validate the exact authenticated judge-path browser site/session without bypassing login/CAPTCHA/MFA/site safeguards.
