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
- `NebiusResearchDeploymentPreflight` enforces exact S3 bucket/prefix ↔ Serverless mount alignment, `READ_WRITE` transport, valid MysteryBox-backed worker credentials, and required verification-key topology.
- `NebiusResearchLiveDryRunPreflight` adds zero-cost validation for digest-pinned worker image, compute/storage shape, RSA strength, and exact client signing/public-key correspondence.
- `NebiusResearchDeploymentManifestBuilder` + `NebiusResearchLivePreflightReporter` create deterministic redacted deployment evidence and secret-version pin coverage without exposing raw infrastructure secrets/identifiers.
- `NebiusResearchLiveConfigurationLoader` now owns the complete live environment/file parsing and preflight construction path, making the paid and zero-cost live configurations directly unit-testable instead of embedding parsing in top-level CLI code.
- `Nvidea.NebiusContractProbe` provides default planner, `--live-research-preflight`, and opt-in real `--live-research` modes. Both live modes now consume the same validated configuration object and emit the same redacted deployment fingerprint.
- `NebiusResearchPassEvidenceBuilder` defines bounded machine-readable redacted PASS evidence. PASS and redacted-manifest persistence use reusable `AtomicTextArtifactWriter` for same-directory crash-safe replacement.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, and strict MysteryBox resource-id validation.

### 2026-09-10 — Current run: testable live configuration boundary
Completed:
- Re-read `progress.md` completely before mutation and inspected recent commits, `Program.cs`, deployment preflight, dry-run preflight, atomic artifact persistence, and existing tests.
- Added `src/Nvidea.Core/Jobs/NebiusResearchLiveConfiguration.cs` with `NebiusResearchLiveConfiguration` and `NebiusResearchLiveConfigurationLoader`.
- Moved required/optional environment parsing, positive integer parsing, bounded polling/timeout parsing, PEM-file loading, output-path validation, MysteryBox secret/version reference construction, object-storage options, Serverless dispatch options, RSA client-public-key derivation, research-question limits, and preflight report creation behind one reusable loader.
- The loader accepts an injected environment reader for deterministic tests and performs no provider/network calls. PEM read failures are converted to bounded/sanitized errors that identify only the configuration variable, not the secret filesystem path.
- Refactored `tools/Nvidea.NebiusContractProbe/Program.cs` so both `--live-research-preflight` and `--live-research` consume `NebiusResearchLiveConfigurationLoader.LoadFromEnvironment()` instead of maintaining top-level parsing helpers.
- Removed direct `File.WriteAllText` redacted-manifest persistence. `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` now uses `AtomicTextArtifactWriter`, matching the crash-safe semantics already used by live PASS evidence.
- Moved live-only poll seconds, total timeout, research question, manifest path, and PASS-evidence path into the validated configuration object, ensuring malformed values are rejected before provider clients are constructed.
- Added `tests/Nvidea.Core.Tests/NebiusResearchLiveConfigurationLoaderTests.cs` covering a valid configuration, runtime bounds, missing required values, oversized/control-character environment data, malformed MysteryBox version pins, missing/oversized PEM files with path-redaction checks, and canonical/existing-directory output-path validation.

Commits this run:
- `4a76679c182156226f7409753b796c0cd0fcfe2a` — extract testable live research configuration loader.
- `bd3d951337ce1494f6e256fdbabe04495dedecb1` — wire contract probe to the shared loader and atomic redacted-manifest persistence.
- `138446820bc3b98a4e762c4f2311682e955c5a99` — add live configuration parser regression tests.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before each mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms preflight and paid live modes now share the exact configuration-construction path and that output paths/runtime limits are validated before Object Storage or Serverless clients are instantiated.
- Static review confirms redacted deployment manifests no longer use incremental direct writes; both manifest and PASS artifacts now use the same-directory atomic writer.
- Static review confirms test fixtures use generated 2048-bit RSA material, a digest-pinned synthetic worker image, aligned bucket/prefix topology, and provider-shaped MysteryBox ids without real credentials.
- `dotnet --info` was checked in the execution runtime and `dotnet` is not installed, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/Nemotron/Tavily execution is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Configuration errors do not print secret values, access keys, PEM bodies, MysteryBox ids/version ids, bucket names, or provider response bodies.
- PEM-path failures deliberately return variable-scoped messages rather than leaking resolved local paths.
- Output destinations must already have an existing parent directory, preventing the evidence path from silently creating arbitrary directory structures.
- Runtime polling and total timeout remain bounded (1-30 seconds and 2-60 minutes respectively), and the synthetic question remains capped at 2000 characters.
- Both live modes remain fail-closed through `NebiusResearchLivePreflightReporter.ValidateAndBuild`; successful parsing alone cannot bypass digest pinning, RSA identity validation, MysteryBox requirements, or Object Storage/mount alignment.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Perform a compile-oriented contract hardening pass around the newly extracted live configuration boundary: add tests for default optional values, total-timeout/question limits, invalid RSA private-key material, and safe failure behavior when the manifest destination is unwritable; then add a deterministic deployment-evidence comparison command or helper that can compare a saved redacted preflight manifest with a later PASS fingerprint without ever requiring raw credentials. This gives judges/operators a simple reproducibility proof while remaining zero-cost and privacy-preserving before the first credential-backed Nebius Serverless run.
