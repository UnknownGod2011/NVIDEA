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
- `NebiusResearchLiveProviderStartup` is a fail-closed provider-construction boundary: the live Object Storage/Serverless factory is invoked only after exact parsed judging destinations pass the final non-destructive preflight.
- `ResearchCloudExecutionCoordinator` is now the narrow production-facing bridge between the durable local research store and `NebiusResearchClientRuntime`. It requires explicit disclosure-versioned approval scoped to the exact current checkpoint before remote dispatch, shares the state-directory lease with local execution, routes remote reconciliation/cancellation by durable provenance, rejects private OS data, and exposes no provider credentials or transport objects.
- `ResearchJobStatus` distinguishes active Nebius execution and ambiguous `DispatchReserved` state from a crashed local stage. Unfinished remote provenance cannot be advertised as local crash recovery; ambiguous dispatch is shown as requiring reconciliation.
- `ResearchJobRuntime` now blocks both local recovery and local cancellation while unfinished remote provenance exists, preventing an ambiguous Serverless create from being converted into a replayable/cancelled local record.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, `--live-research`, zero-cost `--live-research-preflight`, reproducible/redacted deployment fingerprints, optional MysteryBox version IDs, machine-readable PASS evidence, reusable atomic artifact persistence, strict MysteryBox resource-id validation, a directly testable live configuration loader, and an independent deployment-evidence verifier CLI.

### 2026-09-10 — Strict judging evidence + startup safety
Added strict evidence JSON ingestion, canonical manifest re-hashing, fixed-time fingerprint comparison, malformed/oversized evidence rejection, RSA signing-capability proof, public/private key-role separation, parser boundary tests, judging-evidence documentation, non-destructive destination writability probing, same manifest/PASS path rejection, the artifact-destination CLI, canonical-path gating in both live modes, and the fail-closed provider-construction seam.

### 2026-09-10 — Current run: production remote-research integration safety
Completed:
- Re-read this progress ledger completely and inspected the current repository state, recent commits/tree, `ResearchJobRuntime`, `NebiusResearchClientRuntime`, two-phase dispatch, result ingestion, research status projection, WPF research flow, and the underlying resumable orchestrator before modifying code.
- Added `src/Nvidea.Core/Jobs/ResearchCloudExecutionCoordinator.cs` with a narrow `IRemoteResearchClientRuntime` contract. The coordinator reads the exact current durable checkpoint, rejects non-pending/non-local/private/approval-bearing/unsupported stages, enforces a positive <=24-hour work-item lifetime, validates exact disclosure-versioned stage approval before any remote call, and then delegates to the existing encrypted two-phase Nebius runtime.
- Made `NebiusResearchClientRuntime` implement `IRemoteResearchClientRuntime`, preserving its existing dispatcher/reconciler/ingestor composition while allowing production integration and tests to depend on a small provider-agnostic lifecycle contract.
- Added remote lifecycle reconciliation routing for `DispatchReserved`, `Dispatched`, and `CancelRequested` provenance plus an explicit remote-cancellation request path. All coordinator mutations are serialized under the same OS-backed state-directory lease used by local durable research operations.
- Fixed `ResearchJobStatus` so a genuine `NebiusServerless + Running` stage is no longer falsely labeled `Interrupted`; remote planning/gather/synthesis now has explicit privacy-safe Nebius status text and cannot advertise local `RunNextStep`.
- Found and closed a higher-severity replay hazard: `DispatchReserved` deliberately remains `Local + Running` while Serverless creation is ambiguous. Previously, once stale, it could satisfy local interrupted-recovery checks and be re-armed, risking duplicate provider work/cost. `ResearchJobStatus.HasUnfinishedRemoteProvenance`, status projection, and `ResearchJobRuntime.RecoverInterruptedAsync` now fail closed and require remote reconciliation instead.
- Closed the corresponding cancellation hazard: an ambiguous remote reservation is no longer advertised as locally cancellable, and `ResearchJobRuntime.CancelAsync` rejects unfinished remote provenance so durable reconciliation evidence cannot be discarded while a Nebius job may exist.
- Added `tests/Nvidea.Core.Tests/ResearchCloudExecutionCoordinatorTests.cs` covering exact-stage approval rejection before remote invocation, successful remote-state projection, private-OS-data rejection, lifetime bounds, and `DispatchReserved` reconciliation routing without local provider execution.
- Added `tests/Nvidea.Core.Tests/ResearchRemoteRecoverySafetyTests.cs` proving a stale ambiguous `DispatchReserved` record is not marked interrupted, cannot advertise local run/recovery/cancel, rejects both local recovery and local cancellation, and preserves its durable remote provenance unchanged.

Commits this run:
- `f05d591943116e2243d1ca6ae01a7770fae457e5` — add production research cloud execution coordinator.
- `103c54bccd857e7c1b5cdb9abd1ea281f38b492f` — expose Nebius client through the narrow remote runtime contract.
- `02d3e4272fd74da241f66cb8ff3713b5a12bf1a4` and `64c44d1fc784f84d2f056ab44141b813662b2c9d` — correct remote status projection and block ambiguous reservations from local recovery semantics.
- `4e1f6aeec6810c0a788c924a7c317f1dd942fadf` — add coordinator regression tests.
- `dcc39579ea3b7ac0f60426562ffba322795b1341` — block ambiguous remote dispatch from `ResearchJobRuntime` local recovery.
- `81f15a60b56e8249100b9d5caa0d9a1a6cafd92b` — add stale remote-reservation recovery safety test.
- `635751dbe8e51c8ba98c995797dd288aa5146a1b` and `594205e5ed9a387d928da356330bbaf72ed2daf6` — fail closed local cancellation and status affordances for ambiguous remote work.
- `3e2fb1c62e925c3a6efb458b62c3da0c5539ea78` — extend the safety regression through local cancellation.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Source review confirms the coordinator validates exact current checkpoint + disclosure approval before invoking `IRemoteResearchClientRuntime.DispatchAsync`.
- Source review confirms remote reconciliation is provenance-driven rather than replaying `ResearchJobHandler` locally.
- Source review confirms active Nebius `Running` records are no longer treated as local interrupted execution and `DispatchReserved` provenance is explicitly excluded from local recovery.
- Source review confirms local cancellation now fails before `ResumableJobOrchestrator.CancelAsync` when unfinished remote provenance is present.
- Regression source covers zero remote dispatch calls for invalid approval/private data/invalid lifetime, correct remote projection for an accepted dispatch, reserved-state reconciliation routing, and preservation of an ambiguous reservation after rejected local recovery/cancellation.
- `dotnet` was checked in this execution environment and is not installed, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were available or used, and no GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Cloud authorization is ephemeral input to the coordinator and must match job id, exact checkpoint step, current disclosure version, and timing bounds; no approval grant is persisted as execution authority.
- `ContainsPrivateOsData` remains a hard local-only boundary before remote invocation and is checked again by `ResearchWorkItemProtector` at the cryptographic dispatch boundary.
- Remote work items contain the existing durable checkpoint payload but are encrypted before shared transport; provider control-plane arguments receive only an opaque id and bounded protocol metadata through the existing dispatcher.
- The coordinator exposes no API keys, MysteryBox ids, Object Storage credentials, PEM content, URLs, research text, or transport handles in status output.
- Ambiguous `DispatchReserved` provenance now has one safe recovery direction: provider-aware reconciliation. Local retry/re-arm/cancel paths cannot erase or replay it.
- Cancellation of an already `Dispatched` remote stage remains provider-aware via `ResearchCloudExecutionCoordinator.RequestCancellationAsync` + later reconciliation; WPF has not yet been wired to this coordinator and therefore must not claim production remote execution.
- The new code is statically reviewed only; compile/runtime errors remain possible until a .NET 8-capable environment executes the focused suite.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; current code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- Exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the hidden worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Production WPF still does not construct/use `ResearchCloudExecutionCoordinator`; remote research must remain disabled there until composition, disclosure UX, lifecycle routing, and the first live contract PASS are proven.
- `ResearchJobRuntime.GetStatusAsync` is intentionally local-only for non-local records; product integration must route remote status/actions through a lifecycle-aware facade rather than bypassing that boundary.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.
- Destination writability can change after preflight due to external ACL/locking changes; final atomic persistence remains the definitive operation.

## Single Best Next Task
If a .NET-capable path becomes available, compile `Nvidea.Core`, `Nvidea.Worker`, the contract probe, and the focused cloud-coordinator/recovery-safety suites first, fixing any compile/runtime issue before further architecture work; then perform the first credential-backed Nebius Serverless live contract with the manifest/PASS workflow. If execution credentials remain unavailable, build a lifecycle-aware product research facade/composition seam that routes local stages to `ResearchJobRuntime` and remote/ambiguous stages to `ResearchCloudExecutionCoordinator`, with explicit disclosure approval and cancellation/reconciliation semantics, but keep actual WPF cloud dispatch disabled until a real live PASS exists.
