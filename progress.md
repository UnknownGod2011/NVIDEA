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
- Deployment preflight enforces exact Object Storage ↔ Serverless mount alignment, READ_WRITE transport, MysteryBox-backed worker credentials, digest-pinned worker image, RSA strength/identity consistency, bounded compute/storage settings, and a redacted reproducible deployment fingerprint.
- `NebiusResearchLiveConfigurationLoader` owns the complete live environment/file parsing and preflight construction path for both zero-cost and paid live modes.
- `Nvidea.NebiusContractProbe` supports cheap planner, `--live-research-preflight`, and explicit real `--live-research` modes. Both live modes consume the same validated configuration object and fingerprint.
- PASS evidence and redacted deployment manifests use `AtomicTextArtifactWriter` for crash-safe same-directory replacement.
- `NebiusResearchDeploymentEvidenceVerifier` + `tools/Nvidea.NebiusEvidenceVerifier` provide a zero-network, zero-secret reproducibility check between saved preflight and later live PASS artifacts.
- Evidence verification rejects unknown JSON members, duplicate property names at any depth, comments, trailing commas, excessive nesting, oversized artifacts, malformed artifacts, self-inconsistent manifests, and cross-artifact fingerprint mismatches.
- Live RSA role validation proves that the client signing PEM can perform a private-key signature and rejects private material in the worker-public-key slot.
- Main README distinguishes the implemented explicit Serverless contract path from still-unverified production WPF remote execution and links the reproducible judging-evidence workflow.
- Judging artifact destinations have a non-destructive writability preflight and standalone zero-network operator CLI.
- Both `--live-research-preflight` and `--live-research` now enforce destination distinctness/writability against the exact canonical paths returned by `NebiusResearchLiveConfigurationLoader` before final artifact persistence; the paid live path performs this before Object Storage or Serverless clients are constructed.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, strict MysteryBox resource-id validation, a directly testable live configuration loader, and an independent deployment-evidence verifier CLI.

### 2026-09-10 — Strict judging evidence + RSA hardening
Added strict evidence JSON ingestion, canonical manifest re-hashing, fixed-time fingerprint comparison, malformed/oversized evidence rejection, RSA signing-capability proof, public/private key-role separation, parser boundary tests, and judging-evidence documentation.

### 2026-09-10 — Artifact destination safety
Added non-destructive destination writability probing, same manifest/PASS path rejection, direct atomic-writer/destination regression tests, and `tools/Nvidea.NebiusArtifactDestinationCheck`.

### 2026-09-10 — Current run: mandatory destination gating in live modes
Completed:
- Re-read this progress ledger completely and inspected the current repository tree, recent commits, `NebiusResearchArtifactDestinationPreflight`, `NebiusResearchLiveConfigurationLoader`, `Nvidea.NebiusContractProbe`, destination tests, and judging-evidence workflow before changing code.
- Added `NebiusResearchArtifactDestinationPreflight.ValidatePaths(...)` so callers can validate the exact already-parsed/canonical manifest and PASS destinations instead of re-reading mutable process environment state.
- Refactored environment-based destination validation to reuse the same path-validation core.
- Integrated destination validation into `--live-research-preflight` before redacted-manifest persistence.
- Integrated destination validation into `--live-research` before redacted-manifest persistence and before construction of Object Storage or Nebius Serverless provider clients.
- Therefore same-path or unwritable judging destinations now fail before paid provider work begins, rather than relying on operators to remember the standalone self-check.
- Updated CLI help/output to make the enforced destination gate explicit without printing destination paths.
- Added direct regression coverage for validating exact parsed paths and rejecting aliased parsed destinations before probe-file creation.
- Updated `docs/judging-evidence.md` with the standalone destination-check command and documented that both live modes enforce the same check automatically.

Commits this run:
- `53e66b3c47f695fc6dbadd0451acc257c87eab2e` — add canonical parsed-path destination validation.
- `cb23ce0c93a3eef87dd8148caa9075e861f4cdad` — enforce destination preflight in both live contract-probe paths.
- `474ad7b80c2a3917455160436fecdd28dd690ac2` — add parsed-path destination regression tests.
- `ae96100169bbbba362822bfaaf699b067608f0e1` — document mandatory/standalone artifact destination checks.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; all writes targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms both live modes call `ValidateArtifactDestinations(configuration)` immediately after loading configuration and before manifest persistence.
- Static review confirms the paid live mode performs that check before creating `NebiusObjectStorageClient`, `NebiusServerlessJobClient`, or dispatching any remote stage.
- Static review confirms `ValidatePaths` works on the exact configuration paths, preventing a second environment read from drifting away from the values the live runtime will later use.
- Static review confirms same-path rejection happens before any writability probe begins and the check still does not create/replace final evidence artifacts.
- `dotnet` is not installed in this execution environment, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- No artifact paths, credentials, secret IDs, bucket identity, PEM contents, or protected research payloads were added to console output.
- Destination probing remains zero-network and non-destructive to configured final artifacts.
- The paid path now fails on destination configuration before provider-client construction, reducing accidental spend and preventing a run that cannot persist its intended evidence.
- Environment-to-runtime TOCTOU is reduced because destination checks operate on the same immutable configuration object later used for persistence.
- This still cannot guarantee a destination remains writable for the entire live run; filesystem ACL/lock state may change after preflight. Final atomic persistence remains authoritative and a persistence failure must still prevent a PASS claim.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; current code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.
- Destination writability can change after preflight due to external ACL/locking changes; final atomic persistence therefore remains the definitive operation.

## Single Best Next Task
Add an explicit provider-construction boundary testable seam around the live contract-probe startup so regression tests can prove destination failure occurs before any Object Storage/Serverless client factory is invoked, then—if a .NET-capable environment becomes available—compile and run the focused destination/configuration/evidence tests before the first credential-backed Serverless contract. After that, prioritize the first real Nebius Serverless live PASS and use its findings to wire proven remote research into the production WPF `ResearchJobRuntime` rather than continuing to grow unverified deployment scaffolding.
