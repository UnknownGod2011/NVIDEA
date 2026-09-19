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
Added release browser verification pinned to the expected GitHub origin, exact clean HEAD, canonical suite and fresh evidence. Integrated qualification into live-demo readiness. Added `judge-recording-gate.ps1`: Windows/PowerShell 7, mandatory browser evidence, verifier self-test, cloud-research readiness and build validation are required. Diagnostic skip switches can run troubleshooting checks but can never mint a recording PASS. Added `run-release-browser-qualification.ps1` so wrong-repository, ambiguous-HEAD, or dirty-source states fail before expensive Chromium execution. Producer-side qualification rechecks exact HEAD and complete tracked/untracked cleanliness after Chromium succeeds, so source drift during the run invalidates evidence.

### 2026-09-19 — persisted memory embedding integrity hardening
Completed:
- Reviewed the layered memory implementation instead of continuing release-script work while Windows execution remains externally blocked.
- Hardened `PersonalMemoryService.InitializeAsync` so persisted embedding data is treated as untrusted durable state: malformed vector/provenance pairs are stripped on load and the sanitized record is persisted back to storage.
- Persisted embeddings are now rejected if the vector is empty, dimensions disagree, provider/model identity is blank, or any vector element is NaN/Infinity.
- Added the same finite-vector and provenance identity checks to the semantic comparison boundary so malformed in-memory state cannot reach cosine scoring even if introduced outside normal write flow.
- Lexical/recency/importance retrieval remains available when semantic state is rejected; no memory content is deleted solely because its embedding metadata is malformed.
- Verified repository metadata immediately before each mutation; target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.

Files changed in latest run:
- `src/Nvidea.Core/Memory/PersonalMemoryService.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected recent commits and the current memory models/service before implementation.
- Static review confirms new sanitization executes while initialization holds the memory gate, before records become queryable, and the existing initialization persistence pass writes sanitized persistent records atomically through the configured store path.
- Existing newly-generated embeddings already pass `ValidateEmbedding`; this change closes the corresponding persisted-state trust boundary and adds defense in depth at comparison time.
- Connector environment cannot execute the .NET test suite or Windows/PowerShell/Chromium, so no executable PASS is claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Browser validation remains fail closed and the release/judge trust chain remains unchanged.
- Memory persistence is now explicitly fail-soft for corrupt semantic metadata: malformed embeddings are discarded while user-authored memory content/provenance survives and deterministic lexical retrieval remains usable.
- This avoids NaN/Infinity poisoning of semantic ranking and prevents incomplete or forged provider/model metadata from being treated as a comparable embedding space.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- The new persisted-memory sanitization needs executable .NET unit coverage; current evidence is static review only.
- SHA-256 browser receipts are integrity bindings, not digital signatures; freshness depends on the producer host clock.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add focused executable unit tests for persisted-memory sanitization (NaN/Infinity, dimension mismatch, missing provenance/provider/model, valid embedding preservation and lexical fallback), run the .NET test suite where execution is available, then return to the clean Windows recording checkout for `run-release-browser-qualification.ps1` and the mandatory judge-recording gate.
