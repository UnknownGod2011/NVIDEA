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
- Judging artifact destinations now have a non-destructive writability preflight and a standalone zero-network operator CLI.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, strict MysteryBox resource-id validation, a directly testable live configuration loader, and an independent deployment-evidence verifier CLI.

### 2026-09-10 — Strict judging evidence + RSA hardening
Added strict evidence JSON ingestion, canonical manifest re-hashing, fixed-time fingerprint comparison, malformed/oversized evidence rejection, RSA signing-capability proof, public/private key-role separation, parser boundary tests, and judging-evidence documentation.

### 2026-09-10 — Current run: artifact destination safety
Completed:
- Re-read this progress ledger completely before mutation and inspected the current repo tree, `AtomicTextArtifactWriter`, live configuration loader, live contract probe, PASS-evidence tests, and existing evidence workflow.
- Hardened `AtomicTextArtifactWriter.ValidateDestination` so directory targets fail closed rather than surviving validation until the eventual replacement operation.
- Added `AtomicTextArtifactWriter.ValidateWritableDestination`, which verifies the destination directory supports create/write/flush/delete using a randomized sibling probe file. It never creates, truncates, or replaces the configured final artifact and cleans the probe best-effort on every path.
- Added direct `AtomicTextArtifactWriterTests` for initial write, existing-file replacement, temp cleanup, directory-target rejection, non-destructive writability probing, preservation of existing final evidence, and missing-parent failure without implicit directory creation.
- Found and fixed a judging-evidence integrity risk: manifest and PASS paths could be configured as the same file, allowing a successful live PASS to overwrite the saved preflight manifest and destroy the evidence pair.
- Added `NebiusResearchArtifactDestinationPreflight`, which reads the two optional judging-artifact destinations, rejects malformed/control-character values, rejects manifest/PASS aliasing with OS-appropriate path comparison, and proves each configured destination is writable without modifying final artifacts.
- Added focused tests confirming no-output behavior, two-destination probing, same-path rejection, preservation of existing artifacts, and cleanup of ephemeral probes.
- Added `tools/Nvidea.NebiusArtifactDestinationCheck`, a standalone zero-network self-check CLI that reads `NVIDEA_LIVE_REDACTED_MANIFEST_PATH` and `NVIDEA_LIVE_PASS_EVIDENCE_PATH`, checks writability/distinctness, prints no path values, and performs no provider calls.

Commits this run:
- `a6d0bb1d8da71ce4d2f7776e300d39ae4a4e63f4` — harden atomic artifact destination validation and add non-destructive writability probing.
- `ae1166ea1f49d7118bbb37f12a612095a12263de` — add direct atomic writer regression tests.
- `53ae932f36556427fdc9c69e384b11914660a42f` — add judging artifact destination preflight.
- `ad3879aaa4b8ca4ae91803ae85a516f2be4351ed` — test destination preflight behavior and artifact preservation.
- `ce71a68308d8e1db5849e374a19450b5eec1cdc5` — add artifact destination self-check tool project.
- `e57d4a25a450b6fa0631cfa49e184829a02335f0` — implement the zero-cost artifact destination self-check CLI.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; all writes targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms `ValidateWritableDestination` only creates a randomized sibling `*.probe.tmp`, writes one byte, flushes it, deletes it, and never opens the configured final artifact for write/truncate/replacement.
- Static review confirms same manifest/PASS paths fail before either writability probe begins, so no final evidence can be silently collapsed into one file.
- Static review confirms the standalone CLI prints only configured/writable status counts and never emits artifact paths, secret values, provider identifiers, or evidence contents.
- `dotnet` is not installed in this execution environment, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Destination failures use generic descriptions and do not include full filesystem paths.
- The self-check does not contact Nebius, Tavily, Object Storage, MysteryBox, or Token Factory.
- Existing final evidence files are deliberately preserved during writability probing.
- Missing directories are never created implicitly.
- Probe-file cleanup is best-effort in `finally`; cleanup failure cannot convert a failed validation into a false success.
- This check proves directory-level create/write/flush/delete capability. It does not guarantee that an existing final file can always be atomically replaced under every OS ACL/locking condition; the real atomic writer remains the authoritative final operation.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; current code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the real contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.
- The new artifact-destination self-check is not yet automatically invoked by the full `--live-research-preflight` / `--live-research` paths; operators can run the standalone CLI now, but integrating the same check into those paths would eliminate the possibility of skipping it.

## Single Best Next Task
Integrate `NebiusResearchArtifactDestinationPreflight` into both `--live-research-preflight` and `--live-research` before any final artifact persistence or provider-client construction, so same-path and unwritable-destination failures cannot be bypassed. Then document the standalone self-check in the judging-evidence flow and, if a .NET-capable environment becomes available, immediately compile and run the focused atomic-writer/destination/configuration/evidence tests before attempting the first credential-backed Serverless contract.
