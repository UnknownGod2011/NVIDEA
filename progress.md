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
- `Nvidea.NebiusContractProbe` provides default planner, `--live-research-preflight`, and opt-in real `--live-research` modes. The live/preflight modes now share one configuration builder and emit the same redacted deployment fingerprint. No production Serverless claim is made until a credential-backed end-to-end PASS exists.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + live preflight
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, and zero-cost `--live-research-preflight`.

### 2026-09-10 — Reproducible/redacted deployment evidence
Added `NebiusResearchDeploymentManifestBuilder` and `NebiusResearchLivePreflightReporter`. The manifest commits to immutable worker image digest, compute/storage topology, RSA public-key fingerprints, and redacted MysteryBox secret-reference identities. Raw access tokens, S3 credentials, project/subnet ids, PEM bodies, bucket names, secret ids/version ids, provider bodies, and research payloads are excluded. Secret references are classified as `primary-version` or `version-pinned`.

### 2026-09-10 — Current run: tie preflight and live PASS to one immutable deployment fingerprint
Completed:
- Re-read `progress.md` completely and inspected the current contract probe, redacted manifest builder/reporter, recent commits, and deployment tests before changing code.
- Refactored `tools/Nvidea.NebiusContractProbe/Program.cs` so both `--live-research-preflight` and `--live-research` build through one `BuildLiveConfiguration()` path rather than duplicating deployment topology and secret-reference construction.
- Added optional per-secret MysteryBox version environment variables while keeping secret-id compatibility:
  - `NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_VERSION_ID`
  - `NVIDEA_LIVE_SECRET_TAVILY_API_KEY_VERSION_ID`
  - `NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_VERSION_ID`
- Each worker secret is now represented as a `NebiusMysteryBoxSecretRef` carrying its required secret id plus an optional explicit version id. Omitting a version preserves development behavior via the primary version; supplying it makes the redacted deployment evidence version-pinned.
- `--live-research-preflight` now emits the deterministic `DeploymentFingerprintSha256`, version-pinned worker-secret count, and `AllWorkerSecretsVersionPinned` status while still performing no cloud/model/Object Storage requests.
- A real `--live-research` PASS now prints the exact same deployment fingerprint generated by the mandatory preflight configuration, cryptographically tying success evidence to the immutable image/topology/public-key/secret-reference commitments.
- Added optional `NVIDEA_LIVE_REDACTED_MANIFEST_PATH`. When explicitly supplied, the probe writes only `NebiusResearchDeploymentManifestBuilder.ToJson(...)` to that path; its parent directory must already exist. The path is not required and no raw credentials/secret ids/bucket name/PEM contents are added to the manifest.
- Added regression coverage proving a fully version-pinned 3/3 worker-secret configuration is reported correctly and its JSON still omits all raw version ids.

Commits this run:
- `b5d4764c5e7dacf5d66623dc395078076a6503c8` — tie live probe to reproducible deployment fingerprint and optional MysteryBox version ids.
- `3c7fc27ef8d53662829d3e94a04ecc6deb03d625` — test fully pinned live deployment evidence.

Validation / evidence:
- Repository identity was explicitly re-verified before every mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms preflight and real live mode now share the same deployment construction path, reducing configuration drift risk.
- Static review confirms the real PASS prints `configuration.Report.Manifest.DeploymentFingerprintSha256`, i.e. the same fingerprint generated by the mandatory zero-cost validation path before provider clients are used.
- Static review confirms optional manifest persistence serializes only the redacted deployment manifest, not the live configuration tuple or raw environment values.
- Existing manifest tests already prove fingerprint determinism, redaction, and fingerprint changes on pinned-secret version rotation; this run adds the missing 3/3 pin-coverage case.
- This runtime still has no usable .NET SDK, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/Nemotron/Tavily execution is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Version ids are accepted only as optional configuration inputs; neither ids nor values are printed.
- Preflight output reveals only a SHA-256 deployment fingerprint and aggregate pinning coverage, not secret-reference commitments individually.
- Redacted-manifest persistence is opt-in and never creates parent directories implicitly, reducing accidental writes to unexpected paths.
- Client private signing material remains local; the worker still receives only the corresponding public verification key.
- Live execution still cannot proceed without passing digest pinning, RSA consistency, MysteryBox presence, compute/storage validation, and exact Object Storage ↔ worker mount alignment first.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` currently performs a normal file write rather than atomic temp-file + replace; acceptable for optional evidence output, but atomic persistence would be stronger.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Harden and automate the reproducible live evidence path before the first paid run: add a small contract-probe-focused testable configuration parser/extractor (instead of top-level environment helpers), validate optional MysteryBox version-id syntax/length separately from secret ids, make redacted manifest persistence atomic, and add a machine-readable PASS evidence envelope containing the deployment fingerprint, timestamp, completed remote-stage count, evidence/citation counts, and no sensitive provider data. Then, when real resources are supplied, run `--live-research-preflight` followed by `--live-research` and persist the exact redacted PASS/failure evidence for judging.
