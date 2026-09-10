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
- Both `--live-research-preflight` and `--live-research` enforce destination distinctness/writability against the exact canonical paths returned by `NebiusResearchLiveConfigurationLoader` before final artifact persistence.
- `NebiusResearchLiveProviderStartup` now forms an explicit fail-closed provider-construction boundary: the live Object Storage/Serverless factory is invoked only after the exact parsed judging destinations pass the final non-destructive preflight.

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
Added non-destructive destination writability probing, same manifest/PASS path rejection, direct atomic-writer/destination regression tests, `tools/Nvidea.NebiusArtifactDestinationCheck`, and mandatory canonical-path destination gating in both live modes.

### 2026-09-10 — Current run: provider-construction fail-closed seam
Completed:
- Re-read this progress ledger completely and inspected the current repository tree, the contract-probe startup path, `NebiusResearchArtifactDestinationPreflight`, `NebiusResearchLiveConfiguration`, provider client construction, and focused destination tests before modifying code.
- Added `src/Nvidea.Core/Jobs/NebiusResearchLiveProviderStartup.cs` as a small, reusable startup seam whose provider factory is unreachable until `NebiusResearchArtifactDestinationPreflight.ValidatePaths(...)` succeeds against the exact canonical destinations already stored in `NebiusResearchLiveConfiguration`.
- Refactored the real `--live-research` path so construction of `NebiusObjectStorageClient`, the S3 protected transport, the Serverless `HttpClient`, and `NebiusServerlessJobClient` occurs inside that guarded provider factory.
- Kept the earlier pre-manifest destination validation, then re-validates immediately at provider construction. This intentionally protects both manifest persistence and the provider boundary, including against destination state changes between those points.
- Added partial-construction cleanup: if a provider constructor fails after Object Storage or the Serverless `HttpClient` has been allocated, already-created disposable resources are disposed before the exception propagates.
- Added `tests/Nvidea.Core.Tests/NebiusResearchLiveProviderStartupTests.cs` proving an aliased manifest/PASS destination yields zero provider-factory invocations and does not create the final evidence file.
- Added the positive ordering regression proving valid writable destinations invoke the provider factory exactly once while the non-destructive preflight still leaves both final evidence files absent.

Commits this run:
- `4828ae747e1ee22ab8a775fc94184ac78fa86646` — add fail-closed live provider startup seam.
- `fa27ae5abb64b30502dbfc891b6e29d5d6153392` — enforce destination gate at the real provider factory boundary and clean up partial construction failures.
- `bf000173c04c52618fecd79a4be85fe8958ce6dd` — add provider-startup ordering regression tests.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Static review confirms the real live path now creates Object Storage and Serverless clients only inside `NebiusResearchLiveProviderStartup.CreateAfterDestinationPreflight(...)`.
- Static review confirms the guarded seam performs destination validation before invoking the supplied factory delegate.
- Regression source inspection confirms the failure test asserts factory invocation count remains `0` for aliased paths; the success test asserts exactly `1` invocation and no final evidence-file creation by preflight.
- Static review confirms partial provider-construction failures dispose any already-created Object Storage client and Serverless `HttpClient` before rethrowing.
- `dotnet` is not installed in this execution environment, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- The new seam accepts only the already-loaded configuration object and does not re-read environment variables, avoiding a second mutable configuration source.
- No credentials, secret IDs, bucket identity, artifact paths, PEM contents, or protected research payloads are logged by the new boundary.
- Destination failures occur before provider construction and therefore before any Object Storage/Serverless network-capable object can be used.
- Provider constructors themselves are expected to be local setup; no claim is made that construction proves provider connectivity.
- Destination writability can still change after the final preflight because of external ACL/lock changes; atomic manifest/PASS persistence remains authoritative and a persistence failure must continue to prevent a PASS claim.

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
Prioritize executable evidence over more scaffolding. If a .NET-capable environment is available, compile `Nvidea.Core`, `Nvidea.Worker`, and the contract-probe tools and run the focused live-configuration/destination/provider-startup/evidence tests first, fixing any compile/runtime issues found. Then perform the first credential-backed Nebius Serverless live contract using the deterministic manifest/PASS workflow. Use the real run findings to wire the proven remote research path into production WPF `ResearchJobRuntime`; if credentials remain unavailable, next strengthen the production integration boundary and its tests rather than adding another standalone preflight layer.
