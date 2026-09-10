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
- `Nvidea.NebiusContractProbe` provides default planner, `--live-research-preflight`, and opt-in real `--live-research` modes. Live/preflight share one configuration builder and emit the same redacted deployment fingerprint.
- `NebiusResearchPassEvidenceBuilder` defines bounded machine-readable redacted PASS evidence with atomic same-directory persistence; the real live probe now invokes it only after completed remote stages plus validated citations.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + live preflight
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, and zero-cost `--live-research-preflight`.

### 2026-09-10 — Reproducible/redacted deployment evidence
Added deterministic deployment manifest/reporter, optional MysteryBox version IDs, shared live/preflight configuration construction, redacted fingerprint output, optional redacted-manifest persistence, and bounded machine-readable PASS evidence primitives.

### 2026-09-10 — Current run: validated live PASS evidence integration
Completed:
- Re-read `progress.md` completely and inspected the current contract-probe success path, PASS evidence primitive, deployment documentation, and repository identity before changing code.
- Updated `tools/Nvidea.NebiusContractProbe/Program.cs` so `NVIDEA_LIVE_PASS_EVIDENCE_PATH` is validated before Object Storage/Serverless/provider clients are constructed, preventing a bad evidence destination from wasting a paid live run.
- After the durable research loop reaches `Completed`, the probe still requires non-empty research evidence and validated citations. Only after those checks does it build `nvidea.nebius.research-pass.v1` using the exact preflight deployment fingerprint, UTC completion time, remote-stage count, evidence count, and validated-citation count.
- If `NVIDEA_LIVE_PASS_EVIDENCE_PATH` is configured, the redacted PASS JSON is persisted through `NebiusResearchPassEvidenceBuilder.PersistAtomically` before the human-readable PASS line is printed. Failure to persist therefore prevents the probe from claiming PASS.
- Preflight, failed remote stage, invalid durable state, timeout/cancellation, empty evidence, and empty-citation paths never invoke PASS persistence.
- The live probe reports only whether machine-readable evidence was requested/persisted; it never prints the configured path.
- Added shared output-path validation for redacted manifest/PASS destinations requiring an existing parent directory and file name.
- Updated `--help` and `docs/nebius-contract-probe.md` with `NVIDEA_LIVE_PASS_EVIDENCE_PATH`, its live-only semantics, redacted schema contents, atomic-write behavior, and explicit failure/cancellation non-write guarantee.

Commits this run:
- `1ea21acfefbb5ced0723a1116b1d8d05fd1b4101` — persist redacted evidence after live research PASS.
- `43c2dde217b1b2a9801125f92049f993c0ca32d7` — document validated live PASS evidence artifact.

Validation / evidence:
- Repository identity was explicitly verified before each mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Fresh source inspection after the code mutation confirmed the live success path builds/persists PASS evidence only after completed-report evidence and citation checks and before printing the human-readable PASS.
- Static control-flow review confirms `NVIDEA_LIVE_PASS_EVIDENCE_PATH` is parsed before provider clients are constructed, while persistence occurs only at the final validated-success boundary.
- Existing `NebiusResearchPassEvidenceTests` cover fingerprint/timestamp/count validation plus atomic replacement and temporary-file cleanup; this run reused that tested persistence primitive rather than adding a parallel writer.
- This runtime has no usable .NET SDK, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/Nemotron/Tavily execution and an actual PASS artifact are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- PASS evidence contains aggregate proof only; it excludes Serverless ids, bucket names, Object Storage credentials, MysteryBox ids/version ids, PEM material, URLs, research text/evidence bodies, and provider responses.
- An invalid output path fails before cloud work begins; no implicit parent-directory creation occurs.
- A persistence failure prevents the CLI from printing the live PASS claim.
- Failure/cancellation paths cannot create a new PASS artifact because the builder/persistence call is below all terminal validation checks.
- One residual operational caveat remains: if an operator reuses a PASS evidence path that already contains an older successful artifact, a later failed run deliberately leaves that older artifact intact. Consumers should therefore bind evidence to the printed deployment fingerprint/timestamp or use a fresh output path per real run.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Live configuration parsing still lives in top-level `Program.cs`; it should move to a directly testable component with stricter MysteryBox secret/version identifier validation.
- `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` still uses direct `File.WriteAllText`; it should be moved onto the same generic atomic file-persistence primitive before judging artifacts are relied upon.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Extract live environment/configuration parsing from `Nvidea.NebiusContractProbe/Program.cs` into a directly testable configuration component. Add strict MysteryBox secret/version-reference syntax validation and focused tests, then move redacted-manifest persistence to a reusable atomic writer. This reduces the remaining pre-paid-run risk without requiring credentials and makes the eventual first real Nebius Serverless PASS configuration reproducible and testable end to end.
