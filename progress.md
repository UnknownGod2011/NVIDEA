# NVIDEA Hackathon Progress

## Mission
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must remain independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Product / Architecture State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport publishes encrypted work items/bindings/results directly through Nebius Object Storage while the worker consumes the same bucket prefix through a Serverless-mounted directory.
- `NebiusResearchDeploymentPreflight` enforces exact S3 bucket/prefix ↔ Serverless mount alignment, `READ_WRITE` transport, MysteryBox-backed worker credentials, and required verification-key topology.
- `NebiusResearchLiveDryRunPreflight` adds zero-cost validation for digest-pinned worker image, compute/storage shape, RSA strength, and exact client signing/public-key correspondence.
- `NebiusResearchDeploymentManifestBuilder` + `NebiusResearchLivePreflightReporter` create deterministic redacted deployment evidence and secret-version pin coverage without exposing raw infrastructure secrets/identifiers.
- `Nvidea.NebiusContractProbe` provides default planner, `--live-research-preflight`, and opt-in real `--live-research` modes. Live/preflight share one configuration builder and emit the same redacted deployment fingerprint. No production Serverless claim is made until a credential-backed end-to-end PASS exists.
- `NebiusResearchPassEvidenceBuilder` now defines bounded, machine-readable redacted PASS evidence and atomic same-directory persistence for the eventual real live run.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + live preflight
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, and zero-cost `--live-research-preflight`.

### 2026-09-10 — Reproducible/redacted deployment evidence
Added deterministic deployment manifest/reporter, optional MysteryBox version IDs, shared live/preflight configuration construction, redacted fingerprint output, and optional redacted-manifest persistence. Raw access tokens, S3 credentials, project/subnet ids, PEM bodies, bucket names, secret ids/version ids, provider bodies, and research payloads remain excluded.

### 2026-09-10 — Current run: machine-readable PASS evidence + atomic persistence
Completed:
- Re-read `progress.md` completely and inspected the current contract probe, deployment manifest/reporter, repository tree, and existing manifest tests before changing code.
- Added `src/Nvidea.Core/Jobs/NebiusResearchPassEvidence.cs`.
- Added schema `nvidea.nebius.research-pass.v1` containing only: deployment fingerprint, UTC completion timestamp, completed remote-stage count, evidence-item count, and validated-citation count.
- Added strict validation: fingerprint must be exactly 64 hex chars; timestamp must be UTC; counts must be positive and bounded; validated citation count cannot exceed evidence count.
- Added deterministic JSON serialization that contains no provider ids, secret references, credentials, bucket names, PEM material, URLs, payloads, research text, or provider response bodies.
- Added same-directory temp-file + overwrite-rename atomic persistence with `WriteThrough`/disk flush and best-effort temp cleanup. Parent directories are never created implicitly.
- Added `tests/Nvidea.Core.Tests/NebiusResearchPassEvidenceTests.cs` covering valid redacted evidence, malformed fingerprints, non-UTC timestamps, invalid counts, atomic replacement, no leaked temp files after success, and refusal when the parent directory does not exist.

Commits this run:
- `a5a9fdd0eb7164c2ad95a52dc2afb246266e4ac2` — add redacted live research PASS evidence model and atomic persistence.
- `0511e658805cbb7d142b610b0903587bea8da0e5` — add PASS evidence validation/persistence regression coverage.

Validation / evidence:
- Repository identity was explicitly verified before every mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms the PASS schema carries only aggregate, judge-useful proof fields and no provider/resource identifiers or protected research data.
- Static review confirms persistence writes a temporary file in the destination directory, flushes it, then atomically renames/overwrites the destination rather than writing the final evidence file in place.
- Existing test project automatically compiles new `.cs` files through the SDK project layout; no project-file mutation was needed.
- This runtime still has no usable .NET SDK, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/Nemotron/Tavily execution is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- The evidence model intentionally cannot carry serverless job IDs, bucket names, secret IDs/version IDs, PEM contents, access keys, research question/evidence bodies, URLs, or provider responses.
- Count bounds prevent absurd/untrusted values from becoming judging evidence.
- Atomic same-directory replacement prevents a crash from leaving a partially written final PASS artifact; a failed write leaves the prior final artifact intact where filesystem semantics permit.
- Temporary-file cleanup is best-effort and cannot mask the primary persistence exception.
- Client private signing material remains local and unchanged.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- `Nvidea.NebiusContractProbe` does not yet call `NebiusResearchPassEvidenceBuilder` after live success; the schema/persistence primitive is implemented and tested structurally, but wiring it into the top-level probe remains.
- Live configuration parsing still lives in top-level `Program.cs`; it should move to a directly testable component with tighter MysteryBox secret/version identifier validation.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Wire `NebiusResearchPassEvidenceBuilder` into `Nvidea.NebiusContractProbe`: add an optional `NVIDEA_LIVE_PASS_EVIDENCE_PATH`, emit/persist the redacted PASS envelope only after every remote stage has completed and citations are validated, use the same deployment fingerprint produced by preflight, and never write a PASS artifact on failure/cancellation. In the same pass, extract environment parsing into a testable configuration component and validate MysteryBox secret/version-reference syntax independently before the first paid credential-backed run.
