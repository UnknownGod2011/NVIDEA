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
- `Nvidea.NebiusContractProbe` provides default planner, `--live-research-preflight`, and opt-in real `--live-research` modes. Live/preflight share one configuration builder and emit the same redacted deployment fingerprint.
- `NebiusResearchPassEvidenceBuilder` defines bounded machine-readable redacted PASS evidence. PASS persistence now delegates to reusable `AtomicTextArtifactWriter` for same-directory crash-safe replacement.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + live preflight
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, and machine-readable PASS evidence.

### 2026-09-10 — Current run: atomic judging artifacts + strict MysteryBox references
Completed:
- Re-read `progress.md` completely before changing code and inspected the current live contract-probe configuration path, deployment preflight, PASS evidence persistence, and related tests.
- Verified current Nebius MysteryBox documentation before tightening assumptions. Nebius documents secret resource ids in the `mbsec-*` form and version ids in the `mbsecver-*` form; version selection is optional when using the primary secret version.
- Added `src/Nvidea.Core/Jobs/AtomicTextArtifactWriter.cs`, a reusable same-directory temp-file + write-through + atomic-replace primitive for already-redacted judging/evidence artifacts. It requires an existing destination directory, never creates directories implicitly, and performs best-effort temp cleanup without masking the original failure.
- Refactored `NebiusResearchPassEvidenceBuilder.PersistAtomically` to delegate to the shared atomic writer rather than maintaining a second persistence implementation.
- Hardened `NebiusResearchDeploymentPreflight.Validate` so each required worker credential is syntactically validated as a MysteryBox reference before any paid live execution can begin.
- Added public `ValidateMysteryBoxSecretReference` with bounded/control-character-safe resource-id validation: secret ids must use `mbsec-...`; version ids must use `mbsecver-...`; NVIDEA's reproducible live configuration requires the owning secret id whenever a version id is pinned.
- Updated `NebiusResearchDeploymentPreflightTests` so fixtures use provider-shaped MysteryBox ids and added focused regression cases for malformed secret ids, malformed version ids, a version pin without its owning secret id, and a valid explicit version pin.

Commits this run:
- `c40d9350bd04f8f8a3f4295ca2a87dbae39eac97` — add reusable atomic artifact writer.
- `56abed6835788707d606b890c9882b56c5348465` — reuse atomic writer for PASS evidence.
- `eab02d6a771b843134b86bacfd924b5f04d4f362` — validate MysteryBox secret reference syntax.
- `dfad35b084e494a93b3a169ffdbbdaaee3a237b2` — add strict MysteryBox preflight regression coverage.

Validation / evidence:
- Repository identity was explicitly verified immediately before every mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Official Nebius MysteryBox documentation was checked during this run: documented examples use `mbsec-*` secret ids and `mbsecver-*` version ids, and primary-version fallback remains provider behavior when no version id is supplied.
- Static review confirms required live worker secret references now fail before Object Storage/Serverless/model clients are created because deployment preflight is part of the existing zero-cost configuration gate.
- Static review confirms PASS evidence retains the same atomic semantics after refactoring because the extracted writer preserves same-directory temp creation, write-through flushing, overwrite move, and best-effort temp cleanup.
- This runtime does not expose a usable .NET SDK, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available, so Object Storage/Serverless/Nemotron/Tavily execution is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- New MysteryBox validation inspects only resource identifiers; it never resolves or logs secret values.
- Error messages name only the affected worker environment variable and reference type, never the actual secret/version id.
- Version-pinned live references now fail closed if the secret id is missing, preventing ambiguous secret-version provenance in NVIDEA's reproducibility manifest.
- The shared atomic writer is intended only for already-redacted artifacts; it does not weaken existing secret-handling boundaries.
- The stricter reference policy is intentionally narrower than the provider's more permissive selector forms because the NVIDEA live path already supplies required secret ids plus optional version ids and needs deterministic evidence.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Live configuration parsing still lives in top-level `Program.cs`; it remains harder to unit-test than the underlying deployment primitives.
- `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` still calls direct `File.WriteAllText` in the probe and has not yet been moved to `AtomicTextArtifactWriter`.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Extract the live environment/configuration parser from `tools/Nvidea.NebiusContractProbe/Program.cs` into a directly testable component and move redacted-manifest persistence onto `AtomicTextArtifactWriter`. Add parser-focused tests for missing/oversized/control-character environment values, numeric bounds, PEM-file handling, output-path validation, and MysteryBox id/version combinations. This is the remaining highest-value zero-cost hardening step before a credential-backed Nebius Serverless PASS.
