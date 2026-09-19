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
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Expanded credential classification across passwords, OTP, PIN, recovery material, API/access/refresh tokens, identity-number labels and standard security/payment autocomplete tokens. Production snapshots suppress password/OTP/payment values before reading DOM values. Hermetic real-Chromium coverage includes sensitive fields, benign controls, accessibility-reference safety and SPA in-place field repurposing.

### 2026-09-18 to 2026-09-19 — executable browser qualification
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated security suite, deterministic failure propagation, fail-closed TRX validation, and payload-free schema-v2 qualification receipts bound to exact TRX bytes with SHA-256. Added independent verifier with canonical fixture pinning, exact receipt/TRX agreement, source provenance, clean-source policy, strict schema/type checks, timestamp validation and freshness support. Added hermetic verifier regression coverage for canonical evidence plus malformed provenance/type/suite/lookalike/TRX-tampering/time cases.

### 2026-09-19 — release and judge recording trust chain
Added release browser verification pinned to the expected GitHub origin, exact clean HEAD, canonical suite and fresh evidence. Integrated qualification into live-demo readiness. Added `judge-recording-gate.ps1`: Windows/PowerShell 7, mandatory browser evidence, verifier self-test, cloud-research readiness and build validation are required. Diagnostic skip switches can run troubleshooting checks but can never mint a recording PASS. The judge runbook requires fresh retained real-Chromium evidence and fail-closed readiness before recording.

### 2026-09-19 — clean-source qualification now fails before expensive Chromium work
Completed:
- Added `scripts/run-release-browser-qualification.ps1` as the canonical producer-side entry point for release/judge browser evidence.
- Before invoking restore/build/Chromium, it requires PowerShell 7 + Windows, resolves Git, pins `origin` to `UnknownGod2011/NVIDEA`, resolves an exact 40-hex HEAD, and requires `git status --porcelain --untracked-files=normal` to succeed and be empty.
- It then launches the existing browser runner in a child `pwsh` with `-SecuritySuite -KeepResults` (and optional `-InstallChromium`) and independently enforces the child exit status.
- This prevents wasting an expensive real-Chromium qualification run on the wrong repository, ambiguous Git provenance, or a dirty checkout that the downstream release gate would necessarily reject.
- Verified repository metadata immediately before each mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/run-release-browser-qualification.ps1` (new)
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected recent commits, current judge gate, current browser runner and source tree before selecting the task.
- Static review confirms release qualification checks repository identity, exact commit identity and working-tree cleanliness before spawning the existing canonical security-suite producer, and rejects any non-zero producer status.
- The existing producer still owns project-pinned Playwright installation, curated fixture execution, TRX semantic validation and schema-v2 receipt generation; no duplicate qualification implementation was introduced.
- Connector environment cannot execute Windows/PowerShell 7/Chromium, so no executable PASS is claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Browser validation fails closed on process failure, missing/empty/skipped evidence, non-passed results and incomplete canonical fixtures. Schema v2 binds receipts to exact TRX bytes; independent verification checks integrity and semantics.
- Release verification pins canonical fixtures, exact repository, clean current HEAD and evidence freshness. The judge gate self-tests its verifier and requires browser qualification, cloud readiness and build validation.
- The new producer-side release wrapper establishes repository/commit/cleanliness provenance before costly Chromium execution; downstream independent verification remains mandatory rather than trusting the wrapper alone.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- SHA-256 binds receipt -> TRX integrity but is not a digital signature; exact source-commit matching and clean-checkout enforcement remain required.
- Freshness depends on the producer host clock; it is not a cryptographic timestamp authority.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
On the clean Windows recording checkout, run `./scripts/run-release-browser-qualification.ps1 -InstallChromium`; retain its emitted evidence directory, then run only `./scripts/judge-recording-gate.ps1 -BrowserEvidenceDirectory <retained-browser-evidence-directory>` without diagnostic skip flags. Fix any mismatch without weakening fail-closed semantics; once it passes, validate the exact authenticated judge-path browser site/session without bypassing login/CAPTCHA/MFA/site safeguards.
