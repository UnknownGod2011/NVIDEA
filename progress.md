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
Added release browser verification pinned to the expected GitHub origin, exact clean HEAD, canonical suite and fresh evidence. Integrated qualification into live-demo readiness. Added `judge-recording-gate.ps1`: Windows/PowerShell 7, mandatory browser evidence, verifier self-test, cloud-research readiness and build validation are required. Diagnostic skip switches can run troubleshooting checks but can never mint a recording PASS. Added `run-release-browser-qualification.ps1` so wrong-repository, ambiguous-HEAD, or dirty-source states fail before expensive Chromium execution.

### 2026-09-19 — qualification now rejects source drift during execution
Completed:
- Hardened `scripts/run-release-browser-qualification.ps1` so the source provenance boundary covers the entire real-Chromium qualification interval rather than only its start.
- After the browser runner succeeds, the wrapper independently resolves HEAD again and requires it to be the exact same 40-hex commit observed before execution.
- It also re-runs `git status --porcelain --untracked-files=normal` and requires the checkout to remain clean.
- A concurrent commit/checkout, editor-generated tracked or untracked change, or inability to re-check Git state now invalidates otherwise-passing Chromium evidence and requires a fresh run.
- Verified repository metadata immediately before each mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `scripts/run-release-browser-qualification.ps1`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected recent commits, the current source tree, memory implementation, browser runner and release qualification wrapper before selecting the task.
- Static control-flow review confirms pre-run origin/HEAD/clean checks remain intact; successful Chromium execution is now followed by exact HEAD equality and post-run cleanliness checks before PASS can be emitted.
- The change does not weaken or duplicate the existing project-pinned Chromium suite, TRX semantic validation, receipt hashing, independent verifier, freshness policy, release gate or judge gate.
- Connector environment cannot execute Windows/PowerShell 7/Chromium, so no executable PASS is claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Browser validation fails closed on process failure, missing/empty/skipped evidence, non-passed results and incomplete canonical fixtures. Schema v2 binds receipts to exact TRX bytes; independent verification checks integrity and semantics.
- Release verification pins canonical fixtures, exact repository, clean current HEAD and evidence freshness. The judge gate self-tests its verifier and requires browser qualification, cloud readiness and build validation.
- Producer-side qualification now checks repository/commit/cleanliness both before and after expensive execution, closing the source-drift window during qualification. Downstream independent verification remains mandatory.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- SHA-256 binds receipt -> TRX integrity but is not a digital signature; exact source-commit matching and clean-checkout enforcement remain required.
- Freshness depends on the producer host clock; it is not a cryptographic timestamp authority.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
On the clean Windows recording checkout, run `./scripts/run-release-browser-qualification.ps1 -InstallChromium`; retain its emitted evidence directory, then run only `./scripts/judge-recording-gate.ps1 -BrowserEvidenceDirectory <retained-browser-evidence-directory>` without diagnostic skip flags. Fix any mismatch without weakening fail-closed semantics; once it passes, validate the exact authenticated judge-path browser site/session without bypassing login/CAPTCHA/MFA/site safeguards.
